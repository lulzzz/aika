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
    public sealed class RedisHistorian : HistorianBase {

        private readonly ILogger<RedisHistorian> _logger;

        internal string RedisKeyPrefix { get; }

        private readonly string _redisSettings;

        internal IConnectionMultiplexer Connection { get; private set; }

        private bool _isInitialized;

        private bool _isInitializing;

        public override bool IsInitialized { get { return _isInitialized; } }


        public override string Description {
            get { return Resources.RedisHistorian_Description; }
        }

        private readonly ConcurrentDictionary<string, object> _properties = new ConcurrentDictionary<string, object>();


        public override IDictionary<string, object> Properties {
            get { return _properties.ToDictionary(x => x.Key, x => x.Value); }
        }


        private readonly ConcurrentDictionary<string, RedisTagDefinition> _tags = new ConcurrentDictionary<string, RedisTagDefinition>();


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


        internal void RunBackgroundTask(Func<CancellationToken, Task> action, params CancellationToken[] cancellationTokens) {
            TaskRunner.RunBackgroundTask(action, cancellationTokens);
        }


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
                _properties[Resources.RedisHistorian_Properties_Connected] = Connection.IsConnected;
            };

            void onConnectionFailed(object sender, ConnectionFailedEventArgs args) {
                _logger?.LogError($"Redis connection failed: {args.EndPoint} (Connection Type = {args.ConnectionType}, Failure Type = {args.FailureType}).", args.Exception);
                _properties[Resources.RedisHistorian_Properties_Connected] = Connection.IsConnected;
            };

            try {
                _isInitializing = true;
                var configurationOptions = ConfigurationOptions.Parse(_redisSettings);
                configurationOptions.ClientName = "Aika";

                Connection = await ConnectionMultiplexer.ConnectAsync(configurationOptions).ConfigureAwait(false);
                Connection.ConnectionRestored += onConnected;
                Connection.ConnectionFailed += onConnectionFailed;

                _properties[Resources.RedisHistorian_Properties_Connected] = Connection.IsConnected;

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


        public override Task<IEnumerable<TagDefinition>> GetTags(ClaimsPrincipal identity, IEnumerable<string> tagIdsOrNames, CancellationToken cancellationToken) {
            var result = _tags.Values.Where(x => tagIdsOrNames.Any(n => String.Equals(n, x.Id, StringComparison.OrdinalIgnoreCase) || String.Equals(n, x.Name, StringComparison.OrdinalIgnoreCase)));
            return Task.FromResult<IEnumerable<TagDefinition>>(result.ToArray());
        }


        #endregion

        #region [ Read Tag Data ]

        public override Task<IDictionary<string, bool>> CanReadTagData(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            var result = new Dictionary<string, bool>();

            foreach (var item in tagNames) {
                result[item] = true;
            }

            return Task.FromResult<IDictionary<string, bool>>(result);
        }


        public override Task<IEnumerable<DataQueryFunction>> GetAvailableDataQueryFunctions(CancellationToken cancellationToken) {
            return Task.FromResult<IEnumerable<DataQueryFunction>>(new DataQueryFunction[0]);
        }


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

        public override Task<IDictionary<string, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<string> tagNames, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        public override Task<IDictionary<string, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<string> tagNames, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        #endregion

        #region [ Write Tag Data ]

        public override Task<IDictionary<string, bool>> CanWriteTagData(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            var result = new Dictionary<string, bool>();

            foreach (var item in tagNames) {
                result[item] = true;
            }

            return Task.FromResult<IDictionary<string, bool>>(result);
        }

        #endregion

        #region [ Tag Management ]

        public override Task<int?> GetTagCount(ClaimsPrincipal identity, CancellationToken cancellationToken) {
            return Task.FromResult<int?>(_tags.Count);
        }


        public override async Task<TagDefinition> CreateTag(ClaimsPrincipal identity, TagSettings tag, CancellationToken cancellationToken) {
            var tagDefinition = await RedisTagDefinition.Create(this, tag, cancellationToken).ConfigureAwait(false);
            _tags[tagDefinition.Id] = tagDefinition;

            return tagDefinition;
        }


        public override Task<TagDefinition> UpdateTag(ClaimsPrincipal identity, string tagId, TagSettings update, CancellationToken cancellationToken) {
            if (tagId == null) {
                throw new ArgumentNullException(nameof(tagId));
            }
            if (update == null) {
                throw new ArgumentNullException(nameof(update));
            }

            if (!_tags.TryGetValue(tagId, out var tag)) {
                throw new ArgumentException(Resources.RedisHistorian_Error_InvalidTagId, nameof(tagId));
            }

            tag.Update(update);

            return Task.FromResult<TagDefinition>(tag);
        }


        public override async Task<bool> DeleteTag(ClaimsPrincipal identity, string tagId, CancellationToken cancellationToken) {
            if (tagId == null) {
                throw new ArgumentNullException(nameof(tagId));
            }

            if (!_tags.TryRemove(tagId, out var tag)) {
                throw new ArgumentException(Resources.RedisHistorian_Error_InvalidTagId, nameof(tagId));
            }

            await tag.Delete(cancellationToken).ConfigureAwait(false);
            return true;
        }

        #endregion

        #region [ State Set Management ]

        public override Task<StateSet> GetStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<StateSet>> GetStateSets(ClaimsPrincipal identity, StateSetFilter filter, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }


        public override Task<StateSet> CreateStateSet(ClaimsPrincipal identity, string name, IEnumerable<StateSetItem> states, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }


        public override Task<StateSet> UpdateStateSet(ClaimsPrincipal identity, string name, IEnumerable<StateSetItem> states, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        
        public override Task<bool> DeleteStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        #endregion
        
    }
}
