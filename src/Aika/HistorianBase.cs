using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aika {

    /// <summary>
    /// Base class that <see cref="IHistorian"/> implementations must inherit from.
    /// </summary>
    public abstract class HistorianBase : IHistorian {

        #region [ Fields / Properties ]

        /// <summary>
        /// Gets a description for the <see cref="IHistorian"/> implementation.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Indicates if the <see cref="IHistorian"/> has finished initializing.
        /// </summary>
        public abstract bool IsInitialized { get; }

        /// <summary>
        /// Gets bespoke properties associated with the <see cref="IHistorian"/> implementation.
        /// </summary>
        public abstract IDictionary<string, object> Properties { get; }

        /// <summary>
        /// Gets the <see cref="ILoggerFactory"/> to use when creating loggers for the historian and 
        /// any underlying components.
        /// </summary>
        protected internal ILoggerFactory LoggerFactory { get; }

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
        public abstract Task Init(CancellationToken cancellationToken);

        #endregion

        #region [ ITagSearch ]

        /// <summary>
        /// Gets the tags that match the specified filter.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="filter">The tag search filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A collection of matching tags.
        /// </returns>
        public abstract Task<IEnumerable<TagDefinition>> GetTags(ClaimsPrincipal identity, TagDefinitionFilter filter, CancellationToken cancellationToken);


        /// <summary>
        /// Gets the definitions for the specified tags.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagIdsOrNames">The names or IDs of the tags to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The definitions of the requested tags.
        /// </returns>
        public abstract Task<IEnumerable<TagDefinition>> GetTags(ClaimsPrincipal identity, IEnumerable<string> tagIdsOrNames, CancellationToken cancellationToken);


        /// <summary>
        /// Gets the state sets that match the specified search filter.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="filter">The state set search filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The state sets defined by the historian, indexed by set name.
        /// </returns>
        public abstract Task<IEnumerable<StateSet>> GetStateSets(ClaimsPrincipal identity, StateSetFilter filter, CancellationToken cancellationToken);


        /// <summary>
        /// Gets the specified state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="name">The name of the state set.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The corresponding <see cref="StateSet"/>.
        /// </returns>
        public abstract Task<StateSet> GetStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken);

        #endregion

        #region [ ITagDataReader ]

        /// <summary>
        /// Tests if the calling identity is allowed to read data from the specified tag names.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The tag names.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary that maps from tag name to authorization result.
        /// </returns>
        public abstract Task<IDictionary<string, bool>> CanReadTagData(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken);


        /// <summary>
        /// Gets the data query functions supported by the historian.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A collection of data query functions.  Common functions are defined in the <see cref="DataQueryFunction"/> class.
        /// </returns>
        public abstract Task<IEnumerable<DataQueryFunction>> GetAvailableDataQueryFunctions(CancellationToken cancellationToken);


        /// <summary>
        /// Retrieves snapshot data from the historian.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The names of the tags to query.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary of snapshot values, indexed by tag name.
        /// </returns>
        public abstract Task<IDictionary<string, TagValue>> ReadSnapshotData(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken);


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
        public abstract Task<IDictionary<string, TagValueCollection>> ReadRawData(ClaimsPrincipal identity, IEnumerable<string> tagNames, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken);


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
        public abstract Task<IDictionary<string, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<string> tagNames, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, CancellationToken cancellationToken);


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
        public abstract Task<IDictionary<string, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<string> tagNames, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken);

        #endregion

        #region [ ITagDataWriter ]

        /// <summary>
        /// Tests if the calling identity is allowed to write data to the specified tag names.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The tag names.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary that maps from tag name to authorization result.
        /// </returns>
        public abstract Task<IDictionary<string, bool>> CanWriteTagData(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken);

        #endregion

        #region [ ITagManager ]

        /// <summary>
        /// Gets the total number of configured tags.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The total tag count.  Implementations can return <see langword="null"/> if they do not 
        /// track this number, or if it is impractical to calculate it.
        /// </returns>
        public abstract Task<int?> GetTagCount(ClaimsPrincipal identity, CancellationToken cancellationToken);


        /// <summary>
        /// Creates a new tag.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tag">The tag definition.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The new tag definition.
        /// </returns>
        public abstract Task<TagDefinition> CreateTag(ClaimsPrincipal identity, TagSettings tag, CancellationToken cancellationToken);


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
        /// <remarks>
        /// Implementers should call <see cref="TagDefinition.Update(TagSettings)"/> to update the target tag.
        /// </remarks>
        public abstract Task<TagDefinition> UpdateTag(ClaimsPrincipal identity, string tagId, TagSettings update, CancellationToken cancellationToken);


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
        public abstract Task<bool> DeleteTag(ClaimsPrincipal identity, string tagId, CancellationToken cancellationToken);


        /// <summary>
        /// Creates a new state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="settings">The state set settings.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A new <see cref="StateSet"/>.
        /// </returns>
        public abstract Task<StateSet> CreateStateSet(ClaimsPrincipal identity, StateSetSettings settings, CancellationToken cancellationToken);


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
        public abstract Task<StateSet> UpdateStateSet(ClaimsPrincipal identity, string name, StateSetSettings settings, CancellationToken cancellationToken);


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
        public abstract Task<bool> DeleteStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken);

        #endregion

    }
}
