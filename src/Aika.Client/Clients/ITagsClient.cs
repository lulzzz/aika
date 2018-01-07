using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika.Client.Dto;

namespace Aika.Client.Clients {
    /// <summary>
    /// Describes a client for querying the Aika API's tag query endpoint
    /// </summary>
    public interface ITagsClient {

        /// <summary>
        /// Performs a tag search.  Authorized using the <c>aika:readtagdata</c> authorization policy.
        /// </summary>
        /// <param name="filter">The tag search filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the matching tag definitions.
        /// </returns>
        Task<IEnumerable<TagDefinitionDto>> GetTags(TagSearchRequest filter, CancellationToken cancellationToken);


        /// <summary>
        /// Gets the available tag state sets.  State-based tags specify a state set that controls the 
        /// possible values for the tag.  Authorized using the <c>aika:readtagdata</c> authorization 
        /// policy.
        /// </summary>
        /// <param name="filter">The state set search filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the matching state sets.
        /// </returns>
        Task<IEnumerable<StateSetDto>> GetStateSets(StateSetSearchRequest filter, CancellationToken cancellationToken);


        /// <summary>
        /// Gets the state set with the specified name.  State-based tags specify a state set that 
        /// controls the possible values for the tag.  Authorized using the <c>aika:readtagdata</c> 
        /// authorization policy.
        /// </summary>
        /// <param name="name">The state set name.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the matching state set.
        /// </returns>
        Task<StateSetDto> GetStateSet(string name, CancellationToken cancellationToken);


        /// <summary>
        /// Reads snapshot values for a set of tags.  Authorized using the <c>aika:readtagdata</c> 
        /// authorization policy.
        /// </summary>
        /// <param name="request">The snapshot data request.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the snapshot values for the tags, indexed by tag name.
        /// </returns>
        Task<IDictionary<string, TagValueDto>> ReadSnapshotValues(SnapshotDataRequest request, CancellationToken cancellationToken);


        /// <summary>
        /// Reads raw data values for a set of tags.  Authorized using the <c>aika:readtagdata</c> 
        /// authorization policy.
        /// </summary>
        /// <param name="request">The raw data request.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the raw data values for the tags, indexed by tag name.
        /// </returns>
        Task<IDictionary<string, HistoricalTagValuesDto>> ReadRawValues(RawDataRequest request, CancellationToken cancellationToken);


        /// <summary>
        /// Reads processed (aggregated) data values for a set of tags.  Authorized using the 
        /// <c>aika:readtagdata</c> authorization policy.
        /// </summary>
        /// <param name="request">The raw data request.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the processed data values for the tags, indexed by tag name.
        /// </returns>
        Task<IDictionary<string, HistoricalTagValuesDto>> ReadProcessedValues(ProcessedDataRequest request, CancellationToken cancellationToken);


        /// <summary>
        /// Writes snapshot values to a set of tags.  Aika will pass these values onto the exception 
        /// and compression filters for the destination tags, if configured.  Authorized using the 
        /// <c>aika:writetagdata</c> authorization policy.
        /// </summary>
        /// <param name="request">The write request.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that returns the write results, indexed by tag name.
        /// </returns>
        Task<IDictionary<string, WriteTagValuesResultDto>> WriteSnapshotValues(WriteTagValuesRequest request, CancellationToken cancellationToken);


        /// <summary>
        /// Inserts values directly into the historian archive (i.e. bypassing the exception and 
        /// compression filters for the destination tags).  This method is intended for back-filling 
        /// gaps in history; to update the current value for a set of tags, use 
        /// <see cref="WriteSnapshotValues(WriteTagValuesRequest, CancellationToken)"/>.  Authorized 
        /// using the <c>aika:writetagdata</c> authorization policy.
        /// </summary>
        /// <param name="request">The write request.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that returns the write results, indexed by tag name.
        /// </returns>
        Task<IDictionary<string, WriteTagValuesResultDto>> InsertArchiveValues(WriteTagValuesRequest request, CancellationToken cancellationToken);

    }
}
