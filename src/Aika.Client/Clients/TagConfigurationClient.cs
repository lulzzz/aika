using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika.Client.Dto;

namespace Aika.Client.Clients {

    /// <summary>
    /// Client for querying the Aika API's tag configuration and state set configuration endpoints.
    /// </summary>
    public sealed class TagConfigurationClient : ITagConfigurationClient {

        /// <summary>
        /// The Aika API client to use.
        /// </summary>
        private readonly ApiClient _client;


        /// <summary>
        /// Creates a new <see cref="TagConfigurationClient"/> object.
        /// </summary>
        /// <param name="client">The Aika API client to use.</param>
        internal TagConfigurationClient(ApiClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        /// <summary>
        /// Gets the total number of tags defined in the historian, if supported by Aika's underlying 
        /// historian implementation.  Authorized using the <c>aika:managetags</c> authorization policy.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the number of tags in the historian, if available.
        /// </returns>
        public async Task<int?> GetTagCount(CancellationToken cancellationToken) {
            const string url = "api/configuration/tags/count";
            var response = await _client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<int?>(cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Performs a tag search and returns extended information about the matching tags.  Authorized 
        /// using the <c>aika:managetags</c> authorization policy.
        /// </summary>
        /// <param name="filter">The tag search filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the matching tag definitions.
        /// </returns>
        public async Task<IEnumerable<TagDefinitionExtendedDto>> GetTags(TagSearchRequest filter, CancellationToken cancellationToken) {
            if (filter == null) {
                throw new ArgumentNullException(nameof(filter));
            }

            const string url = "api/configuration/tags/search";
            var response = await _client.PostAsJsonAsync(url, filter, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<IEnumerable<TagDefinitionExtendedDto>>(cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets the extended definition for the tag with the specified ID.  Authorized using the 
        /// <c>aika:managetags</c> authorization policy.
        /// </summary>
        /// <param name="tagId">The tag ID.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the matching tag definition.
        /// </returns>
        public async Task<TagDefinitionExtendedDto> GetTag(string tagId, CancellationToken cancellationToken) {
            if (tagId == null) {
                throw new ArgumentNullException(nameof(tagId));
            }

            var url = $"api/configuration/tags/{Uri.EscapeDataString(tagId)}";
            var response = await _client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<TagDefinitionExtendedDto>(cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Creates a new tag in the historian.  Authorized using the <c>aika:managetags</c> 
        /// authorization policy.
        /// </summary>
        /// <param name="tagDefinition">The tag definition to create.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the extended definition for the new tag.
        /// </returns>
        public async Task<TagDefinitionExtendedDto> CreateTag(TagDefinitionUpdateDto tagDefinition, CancellationToken cancellationToken) {
            if (tagDefinition == null) {
                throw new ArgumentNullException(nameof(tagDefinition));
            }

            const string url = "api/configuration/tags";
            var response = await _client.PostAsJsonAsync(url, tagDefinition, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<TagDefinitionExtendedDto>(cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Updates an existing historian tag.  Authorized using the <c>aika:managetags</c> 
        /// authorization policy.
        /// </summary>
        /// <param name="tagId">The ID of the tag to update.</param>
        /// <param name="tagDefinition">The updated tag definition.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the extended definition for the updated tag.
        /// </returns>
        public async Task<TagDefinitionExtendedDto> UpdateTag(string tagId, TagDefinitionUpdateDto tagDefinition, CancellationToken cancellationToken) {
            if (tagId == null) {
                throw new ArgumentNullException(nameof(tagId));
            }
            if (tagDefinition == null) {
                throw new ArgumentNullException(nameof(tagDefinition));
            }

            var url = $"api/configuration/tags/{Uri.EscapeDataString(tagId)}";
            var response = await _client.PutAsJsonAsync(url, tagDefinition, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<TagDefinitionExtendedDto>(cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Deletes a historian tag.  Authorized using the <c>aika:managetags</c> authorization policy.
        /// </summary>
        /// <param name="tagId">The ID of the tag to delete.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return a flag indicating if the delete was successful.
        /// </returns>
        public async Task<bool> DeleteTag(string tagId, CancellationToken cancellationToken) {
            if (tagId == null) {
                throw new ArgumentNullException(nameof(tagId));
            }

            var url = $"api/configuration/tags/{Uri.EscapeDataString(tagId)}";
            var response = await _client.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<bool>(cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets the available tag state sets.  State-based tags specify a state set that controls the 
        /// possible values for the tag.  Authorized using the <c>aika:managetags</c> authorization 
        /// policy.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return all of the defined state sets, indexed by name.
        /// </returns>
        public async Task<IDictionary<string, StateSetDto>> GetStateSets(CancellationToken cancellationToken) {
            const string url = "api/configuration/statesets";
            var response = await _client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<IDictionary<string, StateSetDto>>(cancellationToken).ConfigureAwait(false);

        }


        /// <summary>
        /// Gets the state set with the specified name.  State-based tags specify a state set that 
        /// controls the possible values for the tag.  Authorized using the <c>aika:managetags</c> 
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

            var url = $"api/configuration/statesets/{Uri.EscapeDataString(name)}";
            var response = await _client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<StateSetDto>(cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Creates a new tag state set.  Authorized using the <c>aika:managetags</c> authorization 
        /// policy.
        /// </summary>
        /// <param name="stateSet">The state set definition.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the new state set definition.
        /// </returns>
        public async Task<StateSetDto> CreateStateSet(StateSetDto stateSet, CancellationToken cancellationToken) {
            if (stateSet == null) {
                throw new ArgumentNullException(nameof(stateSet));
            }

            const string url = "api/configuration/statesets";
            var response = await _client.PostAsJsonAsync(url, stateSet, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<StateSetDto>(cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Updates a tag state set.  Authorized using the <c>aika:managetags</c> authorization 
        /// policy.
        /// </summary>
        /// <param name="stateSet">The updated state set.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the updated state set.
        /// </returns>
        public async Task<StateSetDto> UpdateStateSet(StateSetDto stateSet, CancellationToken cancellationToken) {
            if (stateSet == null) {
                throw new ArgumentNullException(nameof(stateSet));
            }

            const string url = "api/configuration/statesets";
            var response = await _client.PutAsJsonAsync(url, stateSet, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<StateSetDto>(cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Deletes a tag state set.  Authorized using the <c>aika:managetags</c> authorization 
        /// policy.
        /// </summary>
        /// <param name="name">The name of the state set to delete.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return a flag indicating if the delete was successful.
        /// </returns>
        public async Task<bool> DeleteStateSet(string name, CancellationToken cancellationToken) {
            if (name == null) {
                throw new ArgumentNullException(nameof(name));
            }

            var url = $"api/configuration/statesets/{Uri.EscapeDataString(name)}";
            var response = await _client.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<bool>(cancellationToken).ConfigureAwait(false);
        }

    }
}
