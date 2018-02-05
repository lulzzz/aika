using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika.Client.Dto;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Aika.Client.Clients {
    /// <summary>
    /// A client that receives snapshot value changes from Aika via a persistent SignalR connection.
    /// </summary>
    public sealed class SnapshotSubscriptionClient : ISnapshotSubscriptionClient {

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger<SnapshotSubscriptionClient> _logger;

        /// <summary>
        /// The <see cref="ApiClient"/> that describes the base HTTP connection settings for the Aika system.
        /// </summary>
        private readonly ApiClient _client;

        /// <summary>
        /// The SignalR hub connection.
        /// </summary>
        private HubConnection _hubConnection;

        /// <summary>
        /// Gets a flag indicating if the client is currently connected to SignalR.
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Raised when the SignalR connection is opened.
        /// </summary>
        public event Func<Task> Connected;

        /// <summary>
        /// Raised when the SignalR connection is closed.
        /// </summary>
        public event Func<Exception, Task> Closed;

        /// <summary>
        /// Raised whenever the client receives snapshot value changes from Aika.
        /// </summary>
        public event SnapshotUpdateDelegate ValuesReceived;


        /// <summary>
        /// Creates a new <see cref="SnapshotSubscriptionClient"/> object.
        /// </summary>
        /// <param name="client">The <see cref="ApiClient"/> that describes the base HTTP connection settings for the Aika system.</param>
        /// <param name="loggerFactory">The logger factory to use.  Can be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
        public SnapshotSubscriptionClient(ApiClient client, ILoggerFactory loggerFactory) {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory?.CreateLogger<SnapshotSubscriptionClient>();
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        /// <summary>
        /// Starts the SignalR connection.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will start the connection.
        /// </returns>
        public async Task Start(CancellationToken cancellationToken) {
            if (IsConnected) {
                return;
            }

            await Stop(cancellationToken).ConfigureAwait(false);

            var connectionBuilder = new HubConnectionBuilder().WithJsonProtocol();
            connectionBuilder.WithMessageHandler(_client.MessageHandler);
            if (_loggerFactory != null) {
                connectionBuilder.WithLoggerFactory(_loggerFactory);
            }

            // If the API client has an Authorization header configured, we'll grab the token and pass 
            // it as a query string parameter to SignalR, since we cannot pass authorization headers 
            // to the websocket.
            if (_client.Authorization != null) {
                connectionBuilder.WithUrl($"{_client.HttpClient.BaseAddress}/hubs/snapshot?token={Uri.EscapeDataString(_client.Authorization.Parameter)}");
            }
            else {
                connectionBuilder.WithUrl($"{_client.HttpClient.BaseAddress}/hubs/snapshot");
            }

            _hubConnection = connectionBuilder.WithMessagePackProtocol().Build();

            _hubConnection.Connected += () => {
                IsConnected = true;
                if (_logger?.IsEnabled(LogLevel.Information) ?? false) {
                    _logger.LogInformation("Connected to SignalR hub.");
                }

                return Task.CompletedTask;
            };
            _hubConnection.Connected += () => Connected != null ? Connected.Invoke() : Task.CompletedTask;

            _hubConnection.Closed += ex => {
                IsConnected = false;
                if (ex != null) {
                    _logger?.LogError("Disconnected from SignalR hub due to an exception.", ex);
                }
                else {
                    if (_logger?.IsEnabled(LogLevel.Information) ?? false) {
                        _logger.LogInformation("Disconnected from SignalR hub.");
                    }
                }
                return Task.CompletedTask;
            };
            _hubConnection.Closed += ex => Closed != null ? Closed.Invoke(ex) : Task.CompletedTask;

            _hubConnection.On<IDictionary<string, TagValueDto[]>>("ValuesReceived", x => {
                if (x?.Count == 0) {
                    return;
                }

                // Invoke ValuesReceived in a background task so that we don't block the websocket 
                // input stream while the handlers are executing.
                Task.Run(() => {
                    if (_logger?.IsEnabled(LogLevel.Trace) ?? false) {
                        _logger.LogTrace($"Received {x.Sum(tag => tag.Value?.Length ?? 0)} values for {x.Count} tags.");
                    }
                    ValuesReceived?.Invoke(x);
                });
            });

            if (_logger?.IsEnabled(LogLevel.Information) ?? false) {
                _logger.LogInformation("Connecting to SignalR hub.");
            }
            await Task.WhenAny(_hubConnection.StartAsync(), Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }


        /// <summary>
        /// Stops the SignalR connection.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will stop the SignalR connection.
        /// </returns>
        public async Task Stop(CancellationToken cancellationToken) {
            if (_hubConnection == null) {
                return;
            }

            if (IsConnected &&( _logger?.IsEnabled(LogLevel.Information) ?? false)) {
                _logger.LogInformation("Disconnecting from SignalR hub.");
            }

            await Task.WhenAny(_hubConnection.DisposeAsync(), Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }


        /// <summary>
        /// Throws an exception if <see cref="IsConnected"/> is <see langword="false"/>.
        /// </summary>
        private void ThrowIfNotConnected() {
            if (!IsConnected) {
                throw new InvalidOperationException($"Not connected.  Call {nameof(Start)}({nameof(CancellationToken)}) to start the connection.");
            }
        }
        

        /// <summary>
        /// Creates snapshot tag subscriptions.
        /// </summary>
        /// <param name="tagNames">The tags to subscribe to.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will update the tag subscriptons.
        /// </returns>
        public async Task Subscribe(IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            ThrowIfNotConnected();
            var distinctTagNames = tagNames?.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? new string[0];
            if (distinctTagNames.Length == 0) {
                return;
            }

            await _hubConnection.SendAsync("Subscribe", new object[] { distinctTagNames }, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }


        /// <summary>
        /// Deletes snapshot tag subscriptions.
        /// </summary>
        /// <param name="tagNames">The tags to unsubscribe from.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will update the tag subscriptons.
        /// </returns>
        public async Task Unsubscribe(IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            ThrowIfNotConnected();
            var distinctTagNames = tagNames?.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? new string[0];
            if (distinctTagNames.Length == 0) {
                return;
            }

            await _hubConnection.SendAsync("Unsubscribe", new object[] { distinctTagNames }, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }


        /// <summary>
        /// Stops the SignalR connection.
        /// </summary>
        public void Dispose() {
            Stop(CancellationToken.None).Wait();
        }
    }
}
