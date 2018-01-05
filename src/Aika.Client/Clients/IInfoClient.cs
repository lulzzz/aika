using System.Threading;
using System.Threading.Tasks;
using Aika.Client.Dto;

namespace Aika.Client.Clients {

    /// <summary>
    /// Describes a client for querying the Aika API's info endpoint.
    /// </summary>
    public interface IInfoClient {

        /// <summary>
        /// Gets basic information about the Aika system.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return basic information about the Aika system.
        /// </returns>
        Task<AikaInfoDto> GetInfo(CancellationToken cancellationToken);

        /// <summary>
        /// Gets extended information about the Aika system.  Authorized using the <c>aika:administrator</c> authorization policy.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return extended information about the Aika system.
        /// </returns>
        Task<AikaInfoExtendedDto> GetInfoExtended(CancellationToken cancellationToken);

    }
}
