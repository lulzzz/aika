using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aika.Client {
    /// <summary>
    /// Delegate for the <see cref="ApiMessageHandler.BeforeSend"/> and <see cref="ApiClient.BeforeSend"/> events.
    /// </summary>
    /// <param name="request">The HTTP request message to be sent.</param>
    /// <param name="cancellationToken">A cancellation token to observe when performed pre-send actions.</param>
    /// <returns>
    /// A task that will perform pre-send actions.
    /// </returns>
    public delegate Task ApiRequestBeforeSendDelegate(HttpRequestMessage request, CancellationToken cancellationToken);
}
