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

    [Authorize(Policy = Authorization.Scopes.Administrator)]
    public class DataFilterHub : Hub {

        /// <summary>
        /// Holds subscriptions for each connection.
        /// </summary>
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<TagDefinition, Subscription>> _subscriptions = new ConcurrentDictionary<string, ConcurrentDictionary<TagDefinition, Subscription>>();

        /// <summary>
        /// The Aika historian to use.
        /// </summary>
        private readonly AikaHistorian _historian;

        
        public DataFilterHub(AikaHistorian historian) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
        }


        public override Task OnConnectedAsync() {
            _subscriptions[Context.ConnectionId] = new ConcurrentDictionary<TagDefinition, Subscription>();

            return base.OnConnectedAsync();
        }


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


        private Action<ExceptionFilterResult> GetExceptionFilterValueProcessedHandler(TagDefinition tag) {
            var client = Clients.Client(Context.ConnectionId);
            return result => {
                var dto = result.ToTagExceptionFilterResultDto(tag);
                client.InvokeAsync("ExceptionFilter", dto);
            };
        }


        private Action<CompressionFilterResult> GetCompressionFilterValueProcessedHandler(TagDefinition tag) {
            var client = Clients.Client(Context.ConnectionId);
            return result => {
                var dto = result.ToTagCompressionFilterResultDto(tag);
                client.InvokeAsync("CompressionFilter", dto);
            };
        }


        public async Task Unsubscribe(IEnumerable<string> tagNames) {
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


        private class Subscription : IDisposable {

            private readonly Action _disposed;


            internal Subscription(Action onDisposed) {
                _disposed = onDisposed;
            }


            public void Dispose() {
                _disposed?.Invoke();
            }

        }

    }

}
