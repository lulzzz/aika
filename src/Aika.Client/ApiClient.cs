using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Aika.Client.Clients;
using Microsoft.Extensions.Logging;

namespace Aika.Client {
    /// <summary>
    /// Client for accessing the Aika API.
    /// </summary>
    public sealed class ApiClient : IDisposable {

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger<ApiClient> _logger;

        /// <summary>
        /// Gets the <see cref="ApiMessageHandler"/> used by the client.
        /// </summary>
        internal ApiMessageHandler MessageHandler { get; }

        /// <summary>
        /// Gets the underlying <see cref="HttpClient"/> that will be used to make API calls.
        /// </summary>
        internal HttpClient HttpClient { get; }

        /// <summary>
        /// Gets or sets the <c>Authorization</c> header value to send as part of any API call.  An 
        /// <c>Authorization</c> header is typically only required if the Aika API is authenticated 
        /// via JWT bearer tokens.
        /// </summary>
        public System.Net.Http.Headers.AuthenticationHeaderValue Authorization { get; set; }

        /// <summary>
        /// Gets the client for querying the Aika API's info endpoint.
        /// </summary>
        public IInfoClient Info { get; }

        /// <summary>
        /// Gets the client for querying the Aika API's tag query endpoint.
        /// </summary>
        public ITagsClient Tags { get; }

        /// <summary>
        /// Gets the client that can be used to subscribe to real-time snapshot value changes.
        /// </summary>
        public ISnapshotSubscriptionClient Snapshot { get; }

        /// <summary>
        /// Gets the client for querying the Aika API's tag configuration endpoint.
        /// </summary>
        public ITagConfigurationClient TagConfiguration { get; }

        /// <summary>
        /// Raised immediately before an HTTP request is sent.
        /// </summary>
        public event ApiRequestBeforeSendDelegate BeforeSend {
            add { MessageHandler.BeforeSend += value; }
            remove { MessageHandler.BeforeSend -= value; }
        }

        /// <summary>
        /// Raised immediately after an HTTP response has been received.
        /// </summary>
        public event ApiRequestAfterResponseDelegate AfterResponse {
            add { MessageHandler.AfterResponse += value; }
            remove { MessageHandler.AfterResponse -= value; }
        }


        /// <summary>
        /// Creates a new <see cref="ApiClient"/> object.
        /// </summary>
        /// <param name="loggerFactory">The logger factory for the API client.  Can be <see langword="null"/>.</param>
        private ApiClient(ILoggerFactory loggerFactory) {
            _logger = loggerFactory?.CreateLogger<ApiClient>();
            Info = new InfoClient(this);
            Tags = new TagsClient(this);
            Snapshot = new SnapshotSubscriptionClient(this, loggerFactory);
            TagConfiguration = new TagConfigurationClient(this);
        }


        /// <summary>
        /// Creates a new <see cref="ApiClient"/> object with the specified base URL.
        /// </summary>
        /// <param name="baseUrl">The base URL of the Aika API to query (e.g. <c>https://aika.myorg.com/aika</c>).</param>
        /// <param name="loggerFactory">The logger factory for the API client.  Can be <see langword="null"/>.</param>
        public ApiClient(string baseUrl, ILoggerFactory loggerFactory) : this(baseUrl, (IWebProxy) null, loggerFactory) { }


        /// <summary>
        /// Creates a new <see cref="ApiClient"/> object with the specified base URL and proxy settings.
        /// </summary>
        /// <param name="baseUrl">The base URL of the Aika API to query (e.g. <c>https://aika.myorg.com/aika</c>).</param>
        /// <param name="proxy">The proxy settings to use.</param>
        /// <param name="loggerFactory">The logger factory for the API client.  Can be <see langword="null"/>.</param>
        public ApiClient(string baseUrl, IWebProxy proxy, ILoggerFactory loggerFactory) : this(baseUrl, null, proxy, loggerFactory) { }


        /// <summary>
        /// Creates a new <see cref="ApiClient"/> object with the specified base URL and credentials.
        /// </summary>
        /// <param name="baseUrl">The base URL of the Aika API to query (e.g. <c>https://aika.myorg.com/aika</c>).</param>
        /// <param name="credentials">The credentials to use when querying the Aika API.</param>
        /// <param name="loggerFactory">The logger factory for the API client.  Can be <see langword="null"/>.</param>
        public ApiClient(string baseUrl, ICredentials credentials, ILoggerFactory loggerFactory) : this(baseUrl, credentials, null, loggerFactory) { }


        /// <summary>
        /// Creates a new <see cref="ApiClient"/> object with the specified base URL, credentials, and proxy settings.
        /// </summary>
        /// <param name="baseUrl">The base URL of the Aika API to query (e.g. <c>https://aika.myorg.com/aika</c>).</param>
        /// <param name="credentials">The credentials to use when querying the Aika API.</param>
        /// <param name="proxy">The proxy settings to use.</param>
        /// <param name="loggerFactory">The logger factory for the API client.  Can be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="baseUrl"/> is <see langword="null"/>.</exception>
        /// <exception cref="UriFormatException"><paramref name="baseUrl"/> is not a valid absolute URL.</exception>
        public ApiClient(string baseUrl, ICredentials credentials, IWebProxy proxy, ILoggerFactory loggerFactory) : this(loggerFactory) {
            if (baseUrl == null) {
                throw new ArgumentNullException(nameof(baseUrl));
            }

            if (!baseUrl.EndsWith("/")) {
                baseUrl = baseUrl + "/";
            }
            var uri = new Uri(baseUrl, UriKind.Absolute);

            MessageHandler = CreateHttpMessageHandler(uri, credentials, proxy);
            MessageHandler.BeforeSend += (request, ct) => {
                if (Authorization != null && request.Headers.Authorization == null) {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(Authorization.Scheme, Authorization.Parameter);
                }

                if (_logger?.IsEnabled(LogLevel.Trace) ?? false) {
                    _logger.LogTrace($"{request.Method.ToString().ToUpperInvariant()} {request.RequestUri}");
                }

                return Task.CompletedTask;
            };
            MessageHandler.AfterResponse += (response, duration, ct) => {
                if (_logger?.IsEnabled(LogLevel.Trace) ?? false) {
                    string formattedDuration;
                    if (duration < TimeSpan.FromSeconds(1)) {
                        formattedDuration = $"{Math.Ceiling(duration.TotalMilliseconds)} ms";
                    }
                    else {
                        formattedDuration = $"{duration.TotalSeconds:0.00} s";
                    }
                    _logger.LogTrace($"{response.RequestMessage.Method.ToString().ToUpperInvariant()} {response.RequestMessage.RequestUri} {(int) response.StatusCode}/{response.ReasonPhrase ?? response.StatusCode.ToString()} {formattedDuration}");
                }

                return Task.CompletedTask;
            };
            HttpClient = new HttpClient(MessageHandler, false) {
                BaseAddress = uri
            };
        }


        /// <summary>
        /// Creates a new <see cref="ApiMessageHandler"/> for use with the <see cref="ApiClient"/>.
        /// </summary>
        /// <param name="baseUrl">The base URL of the Aika API to query.</param>
        /// <param name="credentials">The credentials to use when querying the Aika API.</param>
        /// <param name="proxy">The proxy settings to use.</param>
        /// <returns>
        /// A new <see cref="ApiMessageHandler"/> instance.
        /// </returns>
        /// <exception cref="UriFormatException"><paramref name="baseUrl"/> is not a valid absolute URL.</exception>
        private static ApiMessageHandler CreateHttpMessageHandler(Uri baseUrl, ICredentials credentials, IWebProxy proxy) {
            var handler = new ApiMessageHandler();
            if (credentials != null) {
                handler.Credentials = credentials;
            }
            if (proxy != null) {
                handler.Proxy = proxy;
            }

            return handler;
        }


        /// <summary>
        /// Releases managed resources.
        /// </summary>
        public void Dispose() {
            Snapshot?.Dispose();
        }

    }
}
