using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika.Client.Dto;

namespace Aika.Client.Clients {

    /// <summary>
    /// Client for querying the Aika API's tag query endpoint
    /// </summary>
    public sealed class TagsClient : ITagsClient {

        /// <summary>
        /// The Aika API client to use.
        /// </summary>
        private readonly ApiClient _client;


        /// <summary>
        /// Creates a new <see cref="TagsClient"/> object.
        /// </summary>
        /// <param name="client">The Aika API client to use.</param>
        internal TagsClient(ApiClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        /// <summary>
        /// Performs a tag search.  Authorized using the <c>aika:readtagdata</c> authorization policy.
        /// </summary>
        /// <param name="filter">The tag search filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the matching tag definitions.
        /// </returns>
        public async Task<IEnumerable<TagDefinitionDto>> GetTags(TagSearchRequest filter, CancellationToken cancellationToken) {
            if (filter == null) {
                throw new ArgumentNullException(nameof(filter));
            }

            const string url = "api/tags";
            var response = await _client.PostAsJsonAsync(url, filter, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<IEnumerable<TagDefinitionDto>>(cancellationToken).ConfigureAwait(false);
        }


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
        public async Task<IEnumerable<StateSetDto>> GetStateSets(StateSetSearchRequest filter, CancellationToken cancellationToken) {
            if (filter == null) {
                throw new ArgumentNullException(nameof(filter));
            }

            const string url = "api/tags/statesets";
            var response = await _client.PostAsJsonAsync(url, filter, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<IEnumerable<StateSetDto>>(cancellationToken).ConfigureAwait(false);
        }


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
        public async Task<StateSetDto> GetStateSet(string name, CancellationToken cancellationToken) {
            if (name == null) {
                throw new ArgumentNullException(nameof(name));
            }

            var url = $"api/tags/statesets/{Uri.EscapeDataString(name)}";
            var response = await _client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<StateSetDto>(cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Reads snapshot values for a set of tags.  Authorized using the <c>aika:readtagdata</c> 
        /// authorization policy.
        /// </summary>
        /// <param name="request">The snapshot data request.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the snapshot values for the tags, indexed by tag name.
        /// </returns>
        public async Task<IDictionary<string, TagValueDto>> ReadSnapshotValues(SnapshotDataRequest request, CancellationToken cancellationToken) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            const string url = "api/tags/data/snapshot";
            var response = await _client.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<IDictionary<string, TagValueDto>>(cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Reads raw data values for a set of tags.  Authorized using the <c>aika:readtagdata</c> 
        /// authorization policy.
        /// </summary>
        /// <param name="request">The raw data request.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the raw data values for the tags, indexed by tag name.
        /// </returns>
        public async Task<IDictionary<string, HistoricalTagValuesDto>> ReadRawValues(RawDataRequest request, CancellationToken cancellationToken) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            const string url = "api/tags/data/raw";
            var response = await _client.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<IDictionary<string, HistoricalTagValuesDto>>(cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Reads visualization-friendly data values for a set of tags.  Authorized using the 
        /// <c>aika:readtagdata</c> authorization policy.
        /// </summary>
        /// <param name="request">The plot data request.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the plot data values for the tags, indexed by tag name.
        /// </returns>
        public async Task<IDictionary<string, HistoricalTagValuesDto>> ReadPlotValues(PlotDataRequest request, CancellationToken cancellationToken) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            const string url = "api/tags/data/plot";
            var response = await _client.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<IDictionary<string, HistoricalTagValuesDto>>(cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Reads processed (aggregated) data values for a set of tags.  Authorized using the 
        /// <c>aika:readtagdata</c> authorization policy.
        /// </summary>
        /// <param name="request">The raw data request.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the processed data values for the tags, indexed by tag name.
        /// </returns>
        public async Task<IDictionary<string, HistoricalTagValuesDto>> ReadProcessedValues(ProcessedDataRequest request, CancellationToken cancellationToken) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            const string url = "api/tags/data/processed";
            var response = await _client.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<IDictionary<string, HistoricalTagValuesDto>>(cancellationToken).ConfigureAwait(false);
        }


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
        public async Task<IDictionary<string, WriteTagValuesResultDto>> WriteSnapshotValues(WriteTagValuesRequest request, CancellationToken cancellationToken) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            const string url = "api/tags/data/write";
            var response = await _client.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<IDictionary<string, WriteTagValuesResultDto>>(cancellationToken).ConfigureAwait(false);
        }


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
        public async Task<IDictionary<string, WriteTagValuesResultDto>> InsertArchiveValues(WriteTagValuesRequest request, CancellationToken cancellationToken) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            const string url = "api/tags/data/write/archive";
            var response = await _client.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<IDictionary<string, WriteTagValuesResultDto>>(cancellationToken).ConfigureAwait(false);
        }
    }
}
