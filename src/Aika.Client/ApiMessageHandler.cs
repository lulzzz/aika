using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aika.Client {
    /// <summary>
    /// <see cref="HttpMessageHandler"/> that allows actions to be executed on a request before it 
    /// is sent, or on a response immediately after it has been received.
    /// </summary>
    public class ApiMessageHandler : HttpClientHandler {

        /// <summary>
        /// Raised immediately before an HTTP request is sent.
        /// </summary>
        public event ApiRequestBeforeSendDelegate BeforeSend;

        /// <summary>
        /// Raised immediately after an HTTP response has been received.
        /// </summary>
        public event ApiRequestAfterResponseDelegate AfterResponse;


        /// <summary>
        /// Sends an HTTP request.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the HTTP response.
        /// </returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            if (BeforeSend != null) {
                await Task.WhenAny(BeforeSend(request, cancellationToken), Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }

            var timer = System.Diagnostics.Stopwatch.StartNew();
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            timer.Stop();

            if (AfterResponse != null) {
                await Task.WhenAny(AfterResponse(response, timer.Elapsed, cancellationToken), Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return response;
        }
    }
}
