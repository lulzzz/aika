using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika.StateSets;
using Aika.Tags;

namespace Aika {

    /// <summary>
    /// Describes a back-end data historian for Aika.
    /// </summary>
    public interface IHistorian {

        #region [ Fields / Properties ]

        /// <summary>
        /// Indicates if the <see cref="IHistorian"/> has finished initializing.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets a description for the <see cref="IHistorian"/> implementation.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets bespoke properties associated with the <see cref="IHistorian"/> implementation.
        /// </summary>
        IDictionary<string, object> Properties { get; }

        #endregion

        #region [ Initialization ]

        /// <summary>
        /// Initializes the <see cref="IHistorian"/>.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel initialization.</param>
        /// <returns>
        /// A task that will initialize the historian.
        /// </returns>
        Task Init(CancellationToken cancellationToken);

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
        Task<IEnumerable<TagDefinition>> GetTags(ClaimsPrincipal identity, TagDefinitionFilter filter, CancellationToken cancellationToken);


        /// <summary>
        /// Gets the definitions for the specified tags.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagIdsOrNames">The names or IDs of the tags to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The definitions of the requested tags, indexed by the entry in <paramref name="tagIdsOrNames"/>.
        /// </returns>
        Task<IDictionary<string, TagDefinition>> GetTags(ClaimsPrincipal identity, IEnumerable<string> tagIdsOrNames, CancellationToken cancellationToken);


        /// <summary>
        /// Gets the state sets that match the specified filter.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="filter">The state set filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The state sets defined by the historian, indexed by set name.
        /// </returns>
        Task<IEnumerable<StateSet>> GetStateSets(ClaimsPrincipal identity, StateSetFilter filter, CancellationToken cancellationToken);


        /// <summary>
        /// Gets the specified state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="name">The name of the state set.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The corresponding <see cref="StateSet"/>.
        /// </returns>
        Task<StateSet> GetStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken);

        #endregion

        #region [ Reading Tag Data ]

        /// <summary>
        /// Gets the data query functions supported by the historian.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A collection of data query functions.  Common functions are defined in the <see cref="DataQueryFunction"/> class.
        /// </returns>
        Task<IEnumerable<DataQueryFunction>> GetAvailableDataQueryFunctions(CancellationToken cancellationToken);


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
        Task<IDictionary<TagDefinition, bool>> IsDataQueryFunctionSupported(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, string dataFunction, CancellationToken cancellationToken);


        /// <summary>
        /// Retrieves snapshot data from the historian.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to query.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary of snapshot values, indexed by tag.
        /// </returns>
        Task<IDictionary<TagDefinition, TagValue>> ReadSnapshotData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, CancellationToken cancellationToken);


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
        Task<IDictionary<TagDefinition, TagValueCollection>> ReadRawData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken);


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
        Task<IDictionary<TagDefinition, TagValueCollection>> ReadPlotData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, DateTime utcStartTime, DateTime utcEndTime, int intervals, CancellationToken cancellationToken);


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
        Task<IDictionary<TagDefinition, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, CancellationToken cancellationToken);


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
        Task<IDictionary<TagDefinition, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken);

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
        Task<TagDefinition> CreateTag(ClaimsPrincipal identity, TagSettings tag, CancellationToken cancellationToken);


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
        /// <remarks>
        /// Implementers should call <see cref="TagDefinition.Update(TagSettings, ClaimsPrincipal, String)"/> to update the target tag.
        /// </remarks>
        Task<TagDefinition> UpdateTag(ClaimsPrincipal identity, TagDefinition tag, TagSettings update, string description, CancellationToken cancellationToken);


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
        Task<bool> DeleteTag(ClaimsPrincipal identity, string tagId, CancellationToken cancellationToken);


        /// <summary>
        /// Creates a new state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="settings">The state set settings.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A new <see cref="StateSet"/>.
        /// </returns>
        Task<StateSet> CreateStateSet(ClaimsPrincipal identity, StateSetSettings settings, CancellationToken cancellationToken);


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
        Task<StateSet> UpdateStateSet(ClaimsPrincipal identity, string name, StateSetSettings settings, CancellationToken cancellationToken);


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
        Task<bool> DeleteStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken);

        #endregion

    }

}
