using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aika.Client {
    /// <summary>
    /// Delegate for the <see cref="ApiMessageHandler.AfterResponse"/> and <see cref="ApiClient.AfterResponse"/> events.
    /// </summary>
    /// <param name="response">The HTTP response that was received.</param>
    /// <param name="duration">The duration of the HTTP request (i.e. the time taken to invoke <see cref="HttpMessageHandler.SendAsync(HttpRequestMessage, CancellationToken)"/>.</param>
    /// <param name="cancellationToken">A cancellation token to observe when performed post-response actions.</param>
    /// <returns>
    /// A task that will perform post-response actions.
    /// </returns>
    public delegate Task ApiRequestAfterResponseDelegate(HttpResponseMessage response, TimeSpan duration, CancellationToken cancellationToken);
}
