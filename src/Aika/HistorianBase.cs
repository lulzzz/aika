using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika.StateSets;
using Aika.Tags;
using Microsoft.Extensions.Logging;

namespace Aika {

    /// <summary>
    /// Base class that <see cref="IHistorian"/> implementations must inherit from.
    /// </summary>
    public abstract class HistorianBase: IHistorian{

        #region [ Fields / Properties ]

        /// <summary>
        /// Gets a description for the <see cref="IHistorian"/> implementation.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Indicates if the <see cref="IHistorian"/> has finished initializing.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// Gets a flag indicating if the <see cref="IHistorian"/> has finished initializing.
        /// </summary>
        bool IHistorian.IsInitialized { get { return _isInitialized; } }

        /// <summary>
        /// Gets the property dictionary for bespoke historian properties.
        /// </summary>
        protected IDictionary<string, object> Properties { get; } = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Gets bespoke properties associated with the <see cref="IHistorian"/> implementation.
        /// </summary>
        IDictionary<string, object> IHistorian.Properties {
            get { return Properties; }
        }

        /// <summary>
        /// Gets the <see cref="ILoggerFactory"/> to use when creating loggers for any underlying 
        /// components.
        /// </summary>
        protected internal ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/> for the historian.  This will be <see langword="null"/> if 
        /// no <see cref="ILoggerFactory"/> was provided when creating the historian.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Gets the <see cref="ITaskRunner"/> that the historian implementation should use to run 
        /// background tasks.
        /// </summary>
        protected internal ITaskRunner TaskRunner { get; }

        #endregion

        #region [ Constructor ]

        /// <summary>
        /// Creates a new <see cref="HistorianBase"/> object.
        /// </summary>
        /// <param name="taskRunner">
        ///   The <see cref="ITaskRunner"/> that the historian implementation should use to run 
        ///   background tasks.
        /// </param>
        /// <param name="loggerFactory">
        ///   Gets the <see cref="ILoggerFactory"/> to use when creating loggers for the historian and 
        ///   any underlying components.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="taskRunner"/> is <see langword="null"/>.</exception>
        protected HistorianBase(ITaskRunner taskRunner, ILoggerFactory loggerFactory) {
            TaskRunner = taskRunner ?? throw new ArgumentNullException(nameof(taskRunner));
            LoggerFactory = loggerFactory;
            Logger = LoggerFactory?.CreateLogger(GetType());
        }

        #endregion

        #region [ Initialization ]

        /// <summary>
        /// Initializes the <see cref="IHistorian"/>.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel initialization.</param>
        /// <returns>
        /// A task that will initialize the historian.
        /// </returns>
        async Task IHistorian.Init(CancellationToken cancellationToken) {
            if (_isInitialized) {
                return;
            }

            try {
                await Init(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                _isInitialized = true;
            }
            catch {
                _isInitialized = false;
                throw;
            }
        }


        /// <summary>
        /// Initializes the <see cref="IHistorian"/>.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel initialization.</param>
        /// <returns>
        /// A task that will initialize the historian.
        /// </returns>
        protected abstract Task Init(CancellationToken cancellationToken);

        #endregion

        #region [ Tag Search ]

        /// <summary>
        /// Gets the tags that match the specified filter.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="filter">The tag search filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A collection of matching tags.
        /// </returns>
        async Task<IEnumerable<TagDefinition>> IHistorian.GetTags(ClaimsPrincipal identity, TagDefinitionFilter filter, CancellationToken cancellationToken) {
            return await GetTags(identity, filter, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets the tags that match the specified filter.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="filter">The tag search filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A collection of matching tags.
        /// </returns>
        protected abstract Task<IEnumerable<TagDefinition>> GetTags(ClaimsPrincipal identity, TagDefinitionFilter filter, CancellationToken cancellationToken);


        /// <summary>
        /// Gets the definitions for the specified tags.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagIdsOrNames">The names or IDs of the tags to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The definitions of the requested tags, indexed by the entry in <paramref name="tagIdsOrNames"/>.
        /// </returns>
        async Task<IDictionary<string, TagDefinition>> IHistorian.GetTags(ClaimsPrincipal identity, IEnumerable<string> tagIdsOrNames, CancellationToken cancellationToken) {
            return await GetTags(identity, tagIdsOrNames, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets the definitions for the specified tags.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagIdsOrNames">The names or IDs of the tags to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The definitions of the requested tags, indexed by the entry in <paramref name="tagIdsOrNames"/>.
        /// </returns>
        protected abstract Task<IDictionary<string, TagDefinition>> GetTags(ClaimsPrincipal identity, IEnumerable<string> tagIdsOrNames, CancellationToken cancellationToken);


        /// <summary>
        /// Gets the state sets that match the specified search filter.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="filter">The state set search filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The state sets defined by the historian, indexed by set name.
        /// </returns>
        async Task<IEnumerable<StateSet>> IHistorian.GetStateSets(ClaimsPrincipal identity, StateSetFilter filter, CancellationToken cancellationToken) {
            return await GetStateSets(identity, filter, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets the state sets that match the specified search filter.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="filter">The state set search filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The state sets defined by the historian, indexed by set name.
        /// </returns>
        protected abstract Task<IEnumerable<StateSet>> GetStateSets(ClaimsPrincipal identity, StateSetFilter filter, CancellationToken cancellationToken);


        /// <summary>
        /// Gets the specified state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="name">The name of the state set.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The corresponding <see cref="StateSet"/>.
        /// </returns>
        async Task<StateSet> IHistorian.GetStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken) {
            return await GetStateSet(identity, name, cancellationToken).ConfigureAwait(false);
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
        protected abstract Task<StateSet> GetStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken);

        #endregion

        #region [ Reading Tag Data ]

        /// <summary>
        /// Gets the data query functions supported by the historian.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A collection of data query functions.  Common functions are defined in the <see cref="DataQueryFunction"/> class.
        /// </returns>
        async Task<IEnumerable<DataQueryFunction>> IHistorian.GetAvailableDataQueryFunctions(CancellationToken cancellationToken) {
            return await GetAvailableDataQueryFunctions(cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets the data query functions supported by the historian.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A collection of data query functions.  Common functions are defined in the <see cref="DataQueryFunction"/> class.
        /// </returns>
        protected abstract Task<IEnumerable<DataQueryFunction>> GetAvailableDataQueryFunctions(CancellationToken cancellationToken);


        /// <summary>
        /// Tests if an aggregate data function is supported by the specified tags.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to query.</param>
        /// <param name="dataFunction">The data query function.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary that maps from tag to a flag indicating if the tag supports the 
        /// <paramref name="dataFunction"/>.
        /// </returns>
        async Task<IDictionary<TagDefinition, bool>> IHistorian.IsDataQueryFunctionSupported(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, string dataFunction, CancellationToken cancellationToken) {
            return await IsDataQueryFunctionSupported(identity, tags, dataFunction, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Tests if an aggregate data function is supported by the specified tags.  The default 
        /// implementation returns <see langword="true"/> for numeric tags, and <see langword="false"/> 
        /// for non-numeric tags.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to query.</param>
        /// <param name="dataFunction">The data query function.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary that maps from tag to a flag indicating if the tag supports the 
        /// <paramref name="dataFunction"/>.
        /// </returns>
        protected virtual Task<IDictionary<TagDefinition, bool>> IsDataQueryFunctionSupported(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, string dataFunction, CancellationToken cancellationToken) {
            var result = tags.ToDictionary(x => x, x => x.DataType == TagDataType.FloatingPoint || x.DataType == TagDataType.Integer);
            return Task.FromResult<IDictionary<TagDefinition, bool>>(result);
        }


        /// <summary>
        /// Retrieves snapshot data from the historian.  The default implementation simply returns 
        /// the <see cref="TagDefinition.SnapshotValue"/> property for each tag; override this 
        /// method in your back-end if you need to query an external service to retrieve snapshot 
        /// values.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to query.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary of snapshot values, indexed by tag.
        /// </returns>
        async Task<IDictionary<TagDefinition, TagValue>> IHistorian.ReadSnapshotData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, CancellationToken cancellationToken) {
            return await ReadSnapshotData(identity, tags, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Retrieves snapshot data from the historian.  The default implementation simply returns 
        /// the <see cref="TagDefinition.SnapshotValue"/> property for each tag; override this 
        /// method in your back-end if you need to query an external service to retrieve snapshot 
        /// values.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to query.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary of snapshot values, indexed by tag.
        /// </returns>
        protected virtual Task<IDictionary<TagDefinition, TagValue>> ReadSnapshotData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, CancellationToken cancellationToken) {
            var result = tags.Where(x => x.Historian == this).ToDictionary(x => x, x => x.SnapshotValue);
            return Task.FromResult<IDictionary<TagDefinition, TagValue>>(result);
        }


        /// <summary>
        /// Reads raw, unprocessed tag data.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to query.</param>
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
        /// A dictionary of historical data, indexed by tag.
        /// </returns>
        async Task<IDictionary<TagDefinition, TagValueCollection>> IHistorian.ReadRawData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken) {
            return await ReadRawData(identity, tags, utcStartTime, utcEndTime, pointCount, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Reads raw, unprocessed tag data.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to query.</param>
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
        /// A dictionary of historical data, indexed by tag.
        /// </returns>
        protected abstract Task<IDictionary<TagDefinition, TagValueCollection>> ReadRawData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken);


        /// <summary>
        /// Reads visualization-friendly plot data.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to query.</param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="intervals">
        ///   The number of intervals to use for the query.  This would typically be the width of the 
        ///   trend that will be visualized, in pixels.  Implementations may return more or fewer samples 
        ///   than this number, depending on the implementation of this method.
        /// </param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary of historical data, indexed by tag.
        /// </returns>
        async Task<IDictionary<TagDefinition, TagValueCollection>> IHistorian.ReadPlotData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, DateTime utcStartTime, DateTime utcEndTime, int intervals, CancellationToken cancellationToken) {
            return await ReadPlotData(identity, tags, utcStartTime, utcEndTime, intervals, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Reads visualization-friendly plot data.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to query.</param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="intervals">
        ///   The number of intervals to use for the query.  This would typically be the width of the 
        ///   trend that will be visualized, in pixels.  Implementations may return more or fewer samples 
        ///   than this number, depending on the implementation of this method.
        /// </param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary of historical data, indexed by tag.
        /// </returns>
        /// <remarks>
        /// Override this method to implement a native plot function.  Do not call this implementation 
        /// in your override.  The default implementation retrieves all raw data between 
        /// <paramref name="utcStartTime"/> and <paramref name="utcEndTime"/>, and then uses Aika's 
        /// built-in plot aggregator to reduce the raw data set.
        /// </remarks>
        protected virtual async Task<IDictionary<TagDefinition, TagValueCollection>> ReadPlotData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, DateTime utcStartTime, DateTime utcEndTime, int intervals, CancellationToken cancellationToken) {
            var rawData = await ReadRawData(identity, tags, utcStartTime, utcEndTime, 0, cancellationToken).ConfigureAwait(false);
            var aggregator = new AggregationUtility(LoggerFactory);

            var result = new Dictionary<TagDefinition, TagValueCollection>();
            foreach (var item in rawData) {
                result[item.Key] = new TagValueCollection() {
                    Values = aggregator.Plot(item.Key, utcStartTime, utcEndTime, intervals, item.Value.Values),
                    VisualizationHint = item.Key.DataType == TagDataType.FloatingPoint || item.Key.DataType == TagDataType.Integer
                        ? TagValueCollectionVisualizationHint.Interpolated
                        : TagValueCollectionVisualizationHint.TrailingEdge
                };
            }

            return result;
        }


        /// <summary>
        /// Performs an aggregated data query on the historian.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to query.</param>
        /// <param name="dataFunction">The data function specifying the type of aggregation to perform on the data.  See <see cref="DataQueryFunction"/> for common function names.</param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="sampleInterval">The sample interval to use for aggregation.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary of historical data, indexed by tag.
        /// </returns>
        /// <seealso cref="DataQueryFunction"/>
        async Task<IDictionary<TagDefinition, TagValueCollection>> IHistorian.ReadProcessedData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, CancellationToken cancellationToken) {
            return await ReadProcessedData(identity, tags, dataFunction, utcStartTime, utcEndTime, sampleInterval, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Performs an aggregated data query on the historian.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to query.</param>
        /// <param name="dataFunction">The data function specifying the type of aggregation to perform on the data.  See <see cref="DataQueryFunction"/> for common function names.</param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="sampleInterval">The sample interval to use for aggregation.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary of historical data, indexed by tag.
        /// </returns>
        /// <seealso cref="DataQueryFunction"/>
        protected abstract Task<IDictionary<TagDefinition, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, CancellationToken cancellationToken);


        /// <summary>
        /// Performs an aggregated data query on the historian.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to query.</param>
        /// <param name="dataFunction">The data function specifying the type of aggregation to perform on the data.  See <see cref="DataQueryFunction"/> for common function names.</param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="pointCount">
        ///   The maximum number of samples to return per tag.  Note that the implementation can apply 
        ///   its own restrictions to this limit.
        /// </param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary of historical data, indexed by tag.
        /// </returns>
        /// <seealso cref="DataQueryFunction"/>
        async Task<IDictionary<TagDefinition, TagValueCollection>> IHistorian.ReadProcessedData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken) {
            return await ReadProcessedData(identity, tags, dataFunction, utcStartTime, utcEndTime, pointCount, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Performs an aggregated data query on the historian.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to query.</param>
        /// <param name="dataFunction">The data function specifying the type of aggregation to perform on the data.  See <see cref="DataQueryFunction"/> for common function names.</param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="pointCount">
        ///   The maximum number of samples to return per tag.  Note that the implementation can apply 
        ///   its own restrictions to this limit.
        /// </param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary of historical data, indexed by tag.
        /// </returns>
        /// <seealso cref="DataQueryFunction"/>
        protected abstract Task<IDictionary<TagDefinition, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken);

        #endregion

        #region [ Tag Management ]

        /// <summary>
        /// Creates a new tag.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tag">The tag definition.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The new tag definition.
        /// </returns>
        async Task<TagDefinition> IHistorian.CreateTag(ClaimsPrincipal identity, TagSettings tag, CancellationToken cancellationToken) {
            return await CreateTag(identity, tag, cancellationToken).ConfigureAwait(false);
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
        protected abstract Task<TagDefinition> CreateTag(ClaimsPrincipal identity, TagSettings tag, CancellationToken cancellationToken);


        /// <summary>
        /// Updates a tag.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tag">The tag to update.</param>
        /// <param name="update">The updated tag definition.</param>
        /// <param name="description">A description of the change.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The updated tag definition.
        /// </returns>
        async Task<TagDefinition> IHistorian.UpdateTag(ClaimsPrincipal identity, TagDefinition tag, TagSettings update, string description, CancellationToken cancellationToken) {
            if (tag.Historian != this) {
                throw new ArgumentException(Resources.Error_CannotOperateOnTagsOwnedByAnotherHistorian, nameof(tag));
            }
            return await UpdateTag(identity, tag, update, description, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Updates a tag.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tag">The tag to update.</param>
        /// <param name="update">The updated tag definition.</param>
        /// <param name="description">A description of the change.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The updated tag definition.
        /// </returns>
        protected virtual Task<TagDefinition> UpdateTag(ClaimsPrincipal identity, TagDefinition tag, TagSettings update, string description, CancellationToken cancellationToken) {
            tag.Update(update, identity, description);
            return Task.FromResult(tag);
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
        async Task<bool> IHistorian.DeleteTag(ClaimsPrincipal identity, string tagId, CancellationToken cancellationToken) {
            return await DeleteTag(identity, tagId, cancellationToken).ConfigureAwait(false);
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
        /// <remarks>
        /// Implementations should return <see langword="false"/> only if the specified 
        /// <paramref name="tagId"/> does not exist.  Delete operations that fail due to 
        /// authorization issues should throw a <see cref="System.Security.SecurityException"/>.
        /// </remarks>
        protected abstract Task<bool> DeleteTag(ClaimsPrincipal identity, string tagId, CancellationToken cancellationToken);


        /// <summary>
        /// Creates a new state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="settings">The state set settings.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A new <see cref="StateSet"/>.
        /// </returns>
        async Task<StateSet> IHistorian.CreateStateSet(ClaimsPrincipal identity, StateSetSettings settings, CancellationToken cancellationToken) {
            return await CreateStateSet(identity, settings, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Creates a new state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="settings">The state set settings.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A new <see cref="StateSet"/>.
        /// </returns>
        protected abstract Task<StateSet> CreateStateSet(ClaimsPrincipal identity, StateSetSettings settings, CancellationToken cancellationToken);


        /// <summary>
        /// Updates an existing state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="name">The name of the state set.</param>
        /// <param name="settings">The state set settings.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The updated <see cref="StateSet"/>.
        /// </returns>
        async Task<StateSet> IHistorian.UpdateStateSet(ClaimsPrincipal identity, string name, StateSetSettings settings, CancellationToken cancellationToken) {
            return await UpdateStateSet(identity, name, settings, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Updates an existing state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="name">The name of the state set.</param>
        /// <param name="settings">The state set settings.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The updated <see cref="StateSet"/>.
        /// </returns>
        protected abstract Task<StateSet> UpdateStateSet(ClaimsPrincipal identity, string name, StateSetSettings settings, CancellationToken cancellationToken);


        /// <summary>
        /// Deletes the specified state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="name">The name of the state set.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A flag that indicates if the state set was deleted.
        /// </returns>
        async Task<bool> IHistorian.DeleteStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken) {
            return await DeleteStateSet(identity, name, cancellationToken).ConfigureAwait(false);
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
        protected abstract Task<bool> DeleteStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken);

        #endregion

    }
}
