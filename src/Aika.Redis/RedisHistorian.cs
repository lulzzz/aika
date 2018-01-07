using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Aika.Redis {

    /// <summary>
    /// Aika historian that uses Redis for persistence.
    /// </summary>
    public sealed class RedisHistorian : HistorianBase {

        #region [ Fields / Properties ]

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger<RedisHistorian> _logger;

        /// <summary>
        /// Gets the Redis key prefix for the historian.
        /// </summary>
        internal string RedisKeyPrefix { get; }

        //The Redis connection settings.
        private readonly string _redisSettings;

        /// <summary>
        /// Gets the Redis connection.
        /// </summary>
        internal IConnectionMultiplexer Connection { get; private set; }

        /// <summary>
        /// Flags if the historian is initialised.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// Flags if the historian is initialising.
        /// </summary>
        private bool _isInitializing;

        /// <summary>
        /// gets a flag that indicates if the historian has finished initializing.
        /// </summary>
        public override bool IsInitialized { get { return _isInitialized; } }

        /// <summary>
        /// Gets the historian description.
        /// </summary>
        public override string Description {
            get { return Resources.Description; }
        }

        /// <summary>
        /// The historian properties.
        /// </summary>
        private readonly ConcurrentDictionary<string, object> _properties = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Gets the historian properties.
        /// </summary>
        public override IDictionary<string, object> Properties {
            get { return _properties.ToDictionary(x => x.Key, x => x.Value); }
        }

        /// <summary>
        /// Tag definitions, indexed by ID.
        /// </summary>
        private readonly ConcurrentDictionary<string, RedisTagDefinition> _tags = new ConcurrentDictionary<string, RedisTagDefinition>();

        /// <summary>
        /// State set definitions, indexed by name.
        /// </summary>
        private readonly ConcurrentDictionary<string, StateSet> _stateSets = new ConcurrentDictionary<string, StateSet>(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region [ Constructor ]

        /// <summary>
        /// Creates a new <see cref="RedisHistorian"/> object.
        /// </summary>
        /// <param name="options">The historian options.</param>
        /// <param name="taskRunner">The task runner for background tasks.</param>
        /// <param name="loggerFactory">The logger factory to use.</param>
        public RedisHistorian(RedisHistorianOptions options, ITaskRunner taskRunner, ILoggerFactory loggerFactory) : base(taskRunner, loggerFactory) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }
            _logger = loggerFactory?.CreateLogger<RedisHistorian>();
            RedisKeyPrefix = String.IsNullOrWhiteSpace(options.KeyPrefix) 
                ? RedisHistorianOptions.DefaultKeyPrefix 
                : options.KeyPrefix;

            _redisSettings = options.RedisConfigurationOptions;
        }

        #endregion

        #region [ Helper Methods ]

        /// <summary>
        /// Runs a background task using the historian's <see cref="ITaskRunner"/>.
        /// </summary>
        /// <param name="action">The action to run in the background.</param>
        /// <param name="cancellationTokens">The additional cancellation tokens that the <paramref name="action"/> should observe.</param>
        internal void RunBackgroundTask(Func<CancellationToken, Task> action, params CancellationToken[] cancellationTokens) {
            TaskRunner.RunBackgroundTask(action, cancellationTokens);
        }

        #endregion

        #region [ Initialization ]

        /// <summary>
        /// Initializes the <see cref="RedisHistorian"/>.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel initialization.</param>
        /// <returns>
        /// A task that will initialize the historian.
        /// </returns>
        public override async Task Init(CancellationToken cancellationToken) {
            if (_isInitialized || _isInitializing) {
                return;
            }

            void onConnected(object sender, ConnectionFailedEventArgs args) {
                _logger?.LogInformation($"Redis connection established: {args.EndPoint} (Connection Type = {args.ConnectionType})");
                _properties[Resources.Properties_Connected] = Connection.IsConnected;
            };

            void onConnectionFailed(object sender, ConnectionFailedEventArgs args) {
                _logger?.LogError($"Redis connection failed: {args.EndPoint} (Connection Type = {args.ConnectionType}, Failure Type = {args.FailureType}).", args.Exception);
                _properties[Resources.Properties_Connected] = Connection.IsConnected;
            };

            try {
                _isInitializing = true;
                var configurationOptions = ConfigurationOptions.Parse(_redisSettings);
                configurationOptions.ClientName = "Aika";

                Connection = await ConnectionMultiplexer.ConnectAsync(configurationOptions).ConfigureAwait(false);
                Connection.ConnectionRestored += onConnected;
                Connection.ConnectionFailed += onConnectionFailed;

                _properties[Resources.Properties_Connected] = Connection.IsConnected;

                // Load state sets first so that they are already loaded when we start loading tags.
                await RedisStateSet.LoadAll(this, stateSet => _stateSets[stateSet.Name] = stateSet, cancellationToken).ConfigureAwait(false);
                await RedisTagDefinition.LoadAll(this, tag => _tags[tag.Id] = tag, cancellationToken).ConfigureAwait(false);

                _isInitialized = true;
            }
            catch {
                if (Connection != null) {
                    Connection.ConnectionRestored -= onConnected;
                    Connection.ConnectionFailed -= onConnectionFailed;
                    Connection.Dispose();
                }
                throw;
            }
            finally {
                _isInitializing = false;
            }
        }

        #endregion

        #region [ Tag Searches ]

        /// <summary>
        /// Gets the tags that match the specified filter.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="filter">The tag search filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A collection of matching tags.
        /// </returns>
        public override Task<IEnumerable<TagDefinition>> GetTags(ClaimsPrincipal identity, TagDefinitionFilter filter, CancellationToken cancellationToken) {
            IEnumerable<TagDefinition> allTags = _tags.Values;
            var result = filter.FilterType == TagDefinitionFilterJoinType.And
                ? (IEnumerable<TagDefinition>) _tags.Values
                : new TagDefinition[0];

            foreach (var clause in filter.FilterClauses) {
                switch (clause.Field) {
                    case TagDefinitionFilterField.Name:
                        if (filter.FilterType == TagDefinitionFilterJoinType.And) {
                            result = result.Where(x => x.Name.Contains(clause.Value));
                        }
                        else {
                            result = result.Concat(allTags.Where(x => x.Name.Contains(clause.Value)));
                        }
                        break;
                    case TagDefinitionFilterField.Description:
                        if (filter.FilterType == TagDefinitionFilterJoinType.And) {
                            result = result.Where(x => x.Description?.Contains(clause.Value) ?? false);
                        }
                        else {
                            result = result.Concat(allTags.Where(x => x.Description?.Contains(clause.Value) ?? false));
                        }
                        break;
                    case TagDefinitionFilterField.Units:
                        if (filter.FilterType == TagDefinitionFilterJoinType.And) {
                            result = result.Where(x => x.Units?.Contains(clause.Value) ?? false);
                        }
                        else {
                            result = result.Concat(allTags.Where(x => x.Units?.Contains(clause.Value) ?? false));
                        }
                        break;
                }
            }

            if (filter.FilterType == TagDefinitionFilterJoinType.Or) {
                result = result.Distinct();
            }

            return Task.FromResult<IEnumerable<TagDefinition>>(result.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToArray());
        }


        /// <summary>
        /// Gets the definitions for the specified tags.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagIdsOrNames">The names or IDs of the tags to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The definitions of the requested tags.
        /// </returns>
        public override Task<IEnumerable<TagDefinition>> GetTags(ClaimsPrincipal identity, IEnumerable<string> tagIdsOrNames, CancellationToken cancellationToken) {
            var result = _tags.Values.Where(x => tagIdsOrNames.Any(n => String.Equals(n, x.Id, StringComparison.OrdinalIgnoreCase) || String.Equals(n, x.Name, StringComparison.OrdinalIgnoreCase)));
            return Task.FromResult<IEnumerable<TagDefinition>>(result.ToArray());
        }

        #endregion

        #region [ Read Tag Data ]

        /// <summary>
        /// Tests if the calling identity is allowed to read data from the specified tag names.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The tag names.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary that maps from tag name to authorization result.
        /// </returns>
        public override Task<IDictionary<string, bool>> CanReadTagData(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            var result = new Dictionary<string, bool>();

            foreach (var item in tagNames) {
                result[item] = true;
            }

            return Task.FromResult<IDictionary<string, bool>>(result);
        }


        /// <summary>
        /// Gets the data query functions supported by the historian.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A collection of data query functions.  Common functions are defined in the <see cref="DataQueryFunction"/> class.
        /// </returns>
        public override Task<IEnumerable<DataQueryFunction>> GetAvailableDataQueryFunctions(CancellationToken cancellationToken) {
            return Task.FromResult<IEnumerable<DataQueryFunction>>(new DataQueryFunction[0]);
        }


        /// <summary>
        /// Retrieves snapshot data from the historian.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The names of the tags to query.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary of snapshot values, indexed by tag name.
        /// </returns>
        public override Task<IDictionary<string, TagValue>> ReadSnapshotData(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            var result = new Dictionary<string, TagValue>(StringComparer.OrdinalIgnoreCase);
            var tags = _tags.Values.Where(x => tagNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase));
            foreach (var tag in tags) {
                if (tag.SnapshotValue == null) {
                    continue;
                }

                result[tag.Name] = tag.SnapshotValue;
            }

            return Task.FromResult<IDictionary<string, TagValue>>(result);
        }


        /// <summary>
        /// Reads raw, unprocessed tag data.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The names of the tags to query.</param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="pointCount">
        ///   The maximum number of samples to return per tag.  Note that the implementation can apply 
        ///   its own restrictions to this limit.  A value of less than one should result in all raw 
        ///   samples between the start and end time being returned, up to the implementation's own 
        ///   internal limit.
        /// </param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary of historical data, indexed by tag name.
        /// </returns>
        public override async Task<IDictionary<string, TagValueCollection>> ReadRawData(ClaimsPrincipal identity, IEnumerable<string> tagNames, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken) {
            var result = new ConcurrentDictionary<string, TagValueCollection>(StringComparer.OrdinalIgnoreCase);
            var tags = _tags.Values.Where(x => tagNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase));

            var tasks = tags.Select(tag => Task.Run(async () => {
                var vals = await tag.GetRawValues(utcStartTime, utcEndTime, pointCount, cancellationToken).ConfigureAwait(false);
                result[tag.Name] = new TagValueCollection() {
                    VisualizationHint = TagValueCollectionVisualizationHint.TrailingEdge,
                    Values = vals.ToArray()
                };
            }));

            await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            return result;
        }


        /// <summary>
        /// Performs an aggregated data query on the historian.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The names of the tags to query.</param>
        /// <param name="dataFunction">The data function specifying the type of aggregation to perform on the data.  See <see cref="DataQueryFunction"/> for common function names.</param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="sampleInterval">The sample interval to use for aggregation.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary of historical data, indexed by tag name.
        /// </returns>
        /// <seealso cref="DataQueryFunction"/>
        public override Task<IDictionary<string, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<string> tagNames, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Performs an aggregated data query on the historian.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The names of the tags to query.</param>
        /// <param name="dataFunction">The data function specifying the type of aggregation to perform on the data.  See <see cref="DataQueryFunction"/> for common function names.</param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="pointCount">
        ///   The maximum number of samples to return per tag.  Note that the implementation can apply 
        ///   its own restrictions to this limit.
        /// </param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary of historical data, indexed by tag name.
        /// </returns>
        /// <seealso cref="DataQueryFunction"/>
        public override Task<IDictionary<string, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<string> tagNames, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        #endregion

        #region [ Write Tag Data ]

        /// <summary>
        /// Tests if the calling identity is allowed to write data to the specified tag names.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The tag names.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary that maps from tag name to authorization result.
        /// </returns>
        public override Task<IDictionary<string, bool>> CanWriteTagData(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            var result = new Dictionary<string, bool>();

            foreach (var item in tagNames) {
                result[item] = true;
            }

            return Task.FromResult<IDictionary<string, bool>>(result);
        }

        #endregion

        #region [ Tag Management ]

        /// <summary>
        /// Gets the total number of configured tags.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The total tag count.  Implementations can return <see langword="null"/> if they do not 
        /// track this number, or if it is impractical to calculate it.
        /// </returns>
        public override Task<int?> GetTagCount(ClaimsPrincipal identity, CancellationToken cancellationToken) {
            return Task.FromResult<int?>(_tags.Count);
        }


        /// <summary>
        /// Creates a new tag.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tag">The tag definition.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The new tag definition.
        /// </returns>
        public override async Task<TagDefinition> CreateTag(ClaimsPrincipal identity, TagSettings tag, CancellationToken cancellationToken) {
            var tagDefinition = await RedisTagDefinition.Create(this, tag, cancellationToken).ConfigureAwait(false);
            _tags[tagDefinition.Id] = tagDefinition;

            return tagDefinition;
        }


        /// <summary>
        /// Updates a tag.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagId">The ID of the tag to update.</param>
        /// <param name="update">The updated tag definition.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The updated tag definition.
        /// </returns>
        public override Task<TagDefinition> UpdateTag(ClaimsPrincipal identity, string tagId, TagSettings update, CancellationToken cancellationToken) {
            if (tagId == null) {
                throw new ArgumentNullException(nameof(tagId));
            }
            if (update == null) {
                throw new ArgumentNullException(nameof(update));
            }

            if (!_tags.TryGetValue(tagId, out var tag)) {
                throw new ArgumentException(Resources.Error_InvalidTagId, nameof(tagId));
            }

            tag.Update(update);

            return Task.FromResult<TagDefinition>(tag);
        }


        /// <summary>
        /// Deletes a tag.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagId">The ID of the tag to delete.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A flag that indicates if the tag was deleted.
        /// </returns>
        public override async Task<bool> DeleteTag(ClaimsPrincipal identity, string tagId, CancellationToken cancellationToken) {
            if (tagId == null) {
                throw new ArgumentNullException(nameof(tagId));
            }

            if (!_tags.TryRemove(tagId, out var tag)) {
                return false;
            }

            await tag.Delete(cancellationToken).ConfigureAwait(false);
            return true;
        }

        #endregion

        #region [ State Set Management ]

        /// <summary>
        /// Gets the state sets that match the specified search filter.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="filter">The state set search filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The state sets defined by the historian, indexed by set name.
        /// </returns>
        public override Task<IEnumerable<StateSet>> GetStateSets(ClaimsPrincipal identity, StateSetFilter filter, CancellationToken cancellationToken) {
            if (filter == null) {
                throw new ArgumentNullException(nameof(filter));
            }

            IEnumerable<StateSet> result = _stateSets.Values;

            if (!String.IsNullOrWhiteSpace(filter.Filter)) {
                result = result.Where(x => x.Name.Contains(filter.Filter));
            }

            result = result.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                           .Skip((filter.Page - 1) * filter.PageSize)
                           .Take(filter.PageSize)
                           .ToArray();

            return Task.FromResult(result);
        }


        /// <summary>
        /// Gets the specified state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="name">The name of the state set.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The corresponding <see cref="StateSet"/>.
        /// </returns>
        public override Task<StateSet> GetStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken) {
            if (String.IsNullOrWhiteSpace(name) || !_stateSets.TryGetValue(name, out var result)) {
                return Task.FromResult<StateSet>(null);
            }

            return Task.FromResult(result);
        }


        /// <summary>
        /// Creates a new state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="name">The name of the new state set.</param>
        /// <param name="states">The states for the set.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A new <see cref="StateSet"/>.
        /// </returns>
        public override async Task<StateSet> CreateStateSet(ClaimsPrincipal identity, string name, IEnumerable<StateSetItem> states, CancellationToken cancellationToken) {
            if (name  == null) {
                throw new ArgumentNullException(nameof(name));
            }

            var stateSet = new StateSet(name, states);
            if (!_stateSets.TryAdd(name, stateSet)) {
                throw new ArgumentException(Resources.Error_StateSetAlreadyExists, nameof(name));
            }
                                                  
            await RedisStateSet.Save(this, stateSet, true, cancellationToken).ConfigureAwait(false);
            return stateSet;
        }


        /// <summary>
        /// Updates an existing state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="name">The name of the state set.</param>
        /// <param name="states">The updated states for the set.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The updated <see cref="StateSet"/>.
        /// </returns>
        public override async Task<StateSet> UpdateStateSet(ClaimsPrincipal identity, string name, IEnumerable<StateSetItem> states, CancellationToken cancellationToken) {
            if (name == null) {
                throw new ArgumentNullException(nameof(name));
            }

            var stateSet = new StateSet(name, states);
            if (!_stateSets.ContainsKey(name)) {
                throw new ArgumentException(Resources.Error_StateSetDoesNotExist, nameof(name));
            }

            _stateSets[name] = stateSet;

            await RedisStateSet.Save(this, stateSet, false, cancellationToken).ConfigureAwait(false);
            return stateSet;
        }


        /// <summary>
        /// Deletes the specified state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="name">The name of the state set.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A flag that indicates if the state set was deleted.
        /// </returns>
        /// <remarks>
        /// Implementations should return <see langword="false"/> only if the specified 
        /// state set <paramref name="name"/> does not exist.  Delete operations that fail due to 
        /// authorization issues should throw a <see cref="System.Security.SecurityException"/>.
        /// </remarks>
        public override async Task<bool> DeleteStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken) {
            if (name == null) {
                throw new ArgumentNullException(nameof(name));
            }

            if (!_stateSets.TryRemove(name, out var removed)) {
                return false;
            }

            await RedisStateSet.Delete(this, removed.Name, cancellationToken).ConfigureAwait(false);
            return true;
        }

        #endregion
        
    }
}
