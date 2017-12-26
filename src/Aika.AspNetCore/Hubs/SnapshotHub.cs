using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Aika.AspNetCore.Hubs {

    /// <summary>
    /// SignalR hub for pushing real-time value changes to subscribers.
    /// </summary>
    [Authorize(Policy = Authorization.Scopes.ReadTagData)]
    public class SnapshotHub : Hub {

        /// <summary>
        /// Holds subscriptions for each connection.
        /// </summary>
        private static readonly ConcurrentDictionary<string, SnapshotSubscription> _subscriptions = new ConcurrentDictionary<string, SnapshotSubscription>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The Aika historian to use.
        /// </summary>
        private readonly AikaHistorian _historian;


        /// <summary>
        /// Creates a new <see cref="SnapshotHub"/> object.
        /// </summary>
        /// <param name="historian">The Aika historian to use.</param>
        public SnapshotHub(AikaHistorian historian) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
        }


        private SnapshotSubscription GetOrAddSubscription() {
            var connectionId = Context.ConnectionId;
            return _subscriptions.GetOrAdd(connectionId, key => {
                var client = Clients.Client(connectionId);
                var subscription = _historian.CreateSnapshotSubscription(Context.User);
                subscription.ValuesReceived += values => {
                    client.InvokeAsync("ValuesReceived", values);
                };

                return subscription;
            });
        }


        private SnapshotSubscription GetSubscription() {
            return _subscriptions.TryGetValue(Context.ConnectionId, out var subscription) 
                ? subscription 
                : null;
        }


        public override Task OnConnectedAsync() {
            GetOrAddSubscription();
            return base.OnConnectedAsync();
        }


        public override Task OnDisconnectedAsync(Exception exception) {
            if (_subscriptions.TryRemove(Context.ConnectionId, out var removed)) {
                removed.Dispose();
            }

            return base.OnDisconnectedAsync(exception);
        }


        public async Task Subscribe(IEnumerable<string> tagNames) {
            var connectionId = Context.ConnectionId;

            var subscription = GetSubscription();
            if (subscription == null) {
                return;
            }
            await subscription.AddTags(Context.User, tagNames, CancellationToken.None).ConfigureAwait(false);
        }


        public async Task Unsubscribe(IEnumerable<string> tagNames) {
            var connectionId = Context.ConnectionId;

            var subscription = GetSubscription();
            if (subscription == null) {
                return;
            }

            await subscription.RemoveTags(Context.User, tagNames, CancellationToken.None).ConfigureAwait(false);
        }

    }
}
