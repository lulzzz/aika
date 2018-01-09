using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika.Client.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Aika.AspNetCore.Hubs {

    /// <summary>
    /// SignalR hub for monitoring the exception and compression filter status for tags.
    /// </summary>
    [Authorize(Policy = Authorization.Policies.Administrator)]
    public class DataFilterHub : Hub {

        /// <summary>
        /// Holds subscriptions for each connection.
        /// </summary>
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<TagDefinition, Subscription>> _subscriptions = new ConcurrentDictionary<string, ConcurrentDictionary<TagDefinition, Subscription>>();

        /// <summary>
        /// The Aika historian to use.
        /// </summary>
        private readonly AikaHistorian _historian;

        
        /// <summary>
        /// Creates a new <see cref="DataFilterHub"/> object.
        /// </summary>
        /// <param name="historian">The Aika historian.</param>
        /// <exception cref="ArgumentNullException"><paramref name="historian"/> is <see langword="null"/>.</exception>
        public DataFilterHub(AikaHistorian historian) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
        }


        /// <summary>
        /// Called when a client connects to the hub.
        /// </summary>
        /// <returns>
        /// A task that will process the connection.
        /// </returns>
        public override Task OnConnectedAsync() {
            _subscriptions[Context.ConnectionId] = new ConcurrentDictionary<TagDefinition, Subscription>();

            return base.OnConnectedAsync();
        }


        /// <summary>
        /// Called when a client disconnects from the hub.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns>
        /// A task that will process the disconnection.
        /// </returns>
        public override Task OnDisconnectedAsync(Exception exception) {
            if (_subscriptions.TryRemove(Context.ConnectionId, out var subscriptions)) {
                foreach (var key in subscriptions.Keys.ToArray()) {
                    if (subscriptions.TryRemove(key, out var item)) {
                        item.Dispose();
                    }
                }
            }

            return base.OnDisconnectedAsync(exception);
        }


        /// <summary>
        /// Subscribes the caller to receive exception and compression filter status updates for the 
        /// specified tag names.
        /// </summary>
        /// <param name="tagNames">The tag names.</param>
        /// <returns>
        /// A task that will process the subscription request.
        /// </returns>
        public async Task Subscribe(IEnumerable<string> tagNames) {
            var subscriptions = _subscriptions[Context.ConnectionId];
            var notSubscribed = tagNames.Where(x => !subscriptions.Keys.Any(t => t.Id.Equals(x) || t.Name.Equals(x, StringComparison.OrdinalIgnoreCase))).ToArray();
            if (notSubscribed.Length == 0) {
                return;
            }

            var tags = await _historian.GetTags(Context.User, notSubscribed, Context.Connection.ConnectionAbortedToken).ConfigureAwait(false);
            foreach (var tag in tags) {
                subscriptions.GetOrAdd(tag, t => {
                    var exceptionFilterHander = GetExceptionFilterValueProcessedHandler(t);
                    var compressionFilterHander = GetCompressionFilterValueProcessedHandler(t);

                    exceptionFilterHander.Invoke(t.DataFilter.ExceptionFilter.LastResult);
                    compressionFilterHander.Invoke(t.DataFilter.CompressionFilter.LastResult);

                    void tagDeleted(TagDefinition deleted) {
                        deleted.Deleted -= tagDeleted;
                        if (subscriptions.TryGetValue(deleted, out var item)) {
                            item.Dispose();
                        }
                    }

                    void subscriptionDisposed() {
                        subscriptions.TryRemove(t, out var _);
                        t.DataFilter.ExceptionFilter.ValueProcessed -= exceptionFilterHander;
                        t.DataFilter.CompressionFilter.ValueProcessed -= compressionFilterHander;
                        t.Deleted -= tagDeleted;
                    }

                    var sub = new Subscription(subscriptionDisposed);

                    t.DataFilter.ExceptionFilter.ValueProcessed += exceptionFilterHander;
                    t.DataFilter.CompressionFilter.ValueProcessed += compressionFilterHander;
                    t.Deleted += tagDeleted;

                    return sub;
                });
            }
        }


        /// <summary>
        /// Creates a delegate that can be used to push exception filter status updates back to the 
        /// calling client.
        /// </summary>
        /// <param name="tag">The tag that the delegate is for.</param>
        /// <returns>
        /// A new exception filter status update delegate.
        /// </returns>
        private Action<ExceptionFilterResult> GetExceptionFilterValueProcessedHandler(TagDefinition tag) {
            var client = Clients.Client(Context.ConnectionId);
            return result => {
                var dto = result.ToTagExceptionFilterResultDto(tag);
                client.InvokeAsync("ExceptionFilter", dto);
            };
        }


        /// <summary>
        /// Creates a delegate that can be used to push compression filter status updates back to the 
        /// calling client.
        /// </summary>
        /// <param name="tag">The tag that the delegate is for.</param>
        /// <returns>
        /// A new compression filter status update delegate.
        /// </returns>
        private Action<CompressionFilterResult> GetCompressionFilterValueProcessedHandler(TagDefinition tag) {
            var client = Clients.Client(Context.ConnectionId);
            return result => {
                var dto = result.ToTagCompressionFilterResultDto(tag);
                client.InvokeAsync("CompressionFilter", dto);
            };
        }


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        /// <summary>
        /// Unsubscribes the caller from the specified tag names.
        /// </summary>
        /// <param name="tagNames">The tag names.</param>
        /// <returns>
        /// A task that will process the request.
        /// </returns>
        public async Task Unsubscribe(IEnumerable<string> tagNames) {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            var subscriptions = _subscriptions[Context.ConnectionId];
            var subscribedTags = subscriptions.Keys.ToArray();
            foreach (var tagName in tagNames) {
                if (String.IsNullOrWhiteSpace(tagName)) {
                    continue;
                }

                var tag = subscribedTags.FirstOrDefault(t => t.Id.Equals(tagName) || t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
                if (tag == null) {
                    continue;
                }

                if (subscriptions.TryGetValue(tag, out var sub)) {
                    sub.Dispose();
                }
            }
        }


        /// <summary>
        /// Subscription object that triggers an action when it is disposed.
        /// </summary>
        private class Subscription : IDisposable {

            /// <summary>
            /// The disposed callback function.
            /// </summary>
            private readonly Action _disposed;


            /// <summary>
            /// Creates a new <see cref="Subscription"/> object.
            /// </summary>
            /// <param name="onDisposed">The callback to invoke when the object is disposed.</param>
            internal Subscription(Action onDisposed) {
                _disposed = onDisposed;
            }


            /// <summary>
            /// Disposes of the subscription and invokes its callback.
            /// </summary>
            public void Dispose() {
                _disposed?.Invoke();
            }

        }

    }

}
