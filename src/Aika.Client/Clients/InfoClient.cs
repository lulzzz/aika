using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika.Client.Dto;

namespace Aika.Client.Clients {

    /// <summary>
    /// A client for querying the Aika API's info endpoint.
    /// </summary>
    public sealed class InfoClient : IInfoClient {

        /// <summary>
        /// The Aika API client.
        /// </summary>
        private readonly ApiClient _client;


        /// <summary>
        /// Creates a new <see cref="InfoClient"/> object.
        /// </summary>
        /// <param name="client">The Aika API client to use.</param>
        internal InfoClient(ApiClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        /// <summary>
        /// Gets basic information about the Aika system.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return basic information about the Aika system.
        /// </returns>
        public async Task<AikaInfoDto> GetInfo(CancellationToken cancellationToken) {
            var response = await _client.GetAsync("api/info", cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<AikaInfoDto>(cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets extended information about the Aika system.  Authorized using the <c>aika:administrator</c> authorization policy.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return extended information about the Aika system.
        /// </returns>
        public async Task<AikaInfoExtendedDto> GetInfoExtended(CancellationToken cancellationToken) {
            var response = await _client.GetAsync("api/info/extended", cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsJsonAsync<AikaInfoExtendedDto>(cancellationToken).ConfigureAwait(false);
        }

    }
}
