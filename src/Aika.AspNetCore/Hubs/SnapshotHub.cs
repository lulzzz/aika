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
    [Authorize(Policy = Authorization.Policies.ReadTagData)]
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


        /// <summary>
        /// Called when a client connects to the hub.
        /// </summary>
        /// <returns>
        /// A task that will register a snapshot subscription for the client.
        /// </returns>
        public override Task OnConnectedAsync() {
            var client = Clients.Client(Context.ConnectionId);
            var subscription = _historian.CreateSnapshotSubscription(Context.User);
            subscription.ValuesReceived += values => {
                client.InvokeAsync("ValuesReceived", values);
            };
            _subscriptions[Context.ConnectionId] = subscription;
            return base.OnConnectedAsync();
        }


        /// <summary>
        /// Called when a client disconnects from the hub.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns>
        /// A task that will unregister the snapshot subscription for the client.
        /// </returns>
        public override Task OnDisconnectedAsync(Exception exception) {
            if (_subscriptions.TryRemove(Context.ConnectionId, out var removed)) {
                removed.Dispose();
            }

            return base.OnDisconnectedAsync(exception);
        }


        /// <summary>
        /// Subscribes the caller to receive snapshot updates for the specified tag names.
        /// </summary>
        /// <param name="tagNames">The tag names to receive updates for.</param>
        /// <returns>
        /// A task that will process the request.
        /// </returns>
        public async Task Subscribe(IEnumerable<string> tagNames) {
            var subscription = _subscriptions[Context.ConnectionId];
            await subscription.AddTags(Context.User, tagNames, Context.Connection.ConnectionAbortedToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Unsubscribes the caller from receiving snapshot updates for the specified tag names.
        /// </summary>
        /// <param name="tagNames">The tag names to unsubscribe from.</param>
        /// <returns>
        /// A task that will process the request.
        /// </returns>
        public async Task Unsubscribe(IEnumerable<string> tagNames) {
            var subscription = _subscriptions[Context.ConnectionId];
            await subscription.RemoveTags(Context.User, tagNames, Context.Connection.ConnectionAbortedToken).ConfigureAwait(false);
        }

    }
}
