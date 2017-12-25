﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aika {
    /// <summary>
    /// Describes a real-time subscription that receives snapshot value changes as they ocur.
    /// </summary>
    public class SnapshotSubscription : IDisposable {

        /// <summary>
        /// Flags if the subscrition has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// The Aika historian for the subscription.
        /// </summary>
        private readonly AikaHistorian _historian;

        /// <summary>
        /// Holds the subscribed tags, indexed by tag ID.
        /// </summary>
        private readonly ConcurrentDictionary<string, TagDefinition> _subscribedTags = new ConcurrentDictionary<string, TagDefinition>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Holds the handlers for the <see cref="TagDefinition.SnapshotValueUpdated"/> events on each subscribed tag.
        /// </summary>
        private readonly ConcurrentDictionary<string, Action<TagValue>> _valueReceivedHandlers = new ConcurrentDictionary<string, Action<TagValue>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Raised whenever the subscription receives new values.
        /// </summary>
        public event SnapshotSubscriptionUpdate ValuesReceived;


        /// <summary>
        /// Creates a new <see cref="SnapshotSubscription"/> object.
        /// </summary>
        /// <param name="historian">The <see cref="AikaHistorian"/> instance to use.</param>
        internal SnapshotSubscription(AikaHistorian historian) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
        }


        /// <summary>
        /// Adds tags to the subscription.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The tags to subscribe to.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will add the tag subscriptions.
        /// </returns>
        public async Task AddTags(ClaimsIdentity identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            var tags = await _historian.GetTags(identity, tagNames, cancellationToken).ConfigureAwait(false);
            foreach (var item in tags) {
                if (!_subscribedTags.TryAdd(item.Id, item)) {
                    continue;
                }

                Action<TagValue> onValueReceived = value => {
                    if (_isDisposed) {
                        return;
                    }

                    ValuesReceived?.Invoke(new Dictionary<string, IEnumerable<TagValue>>(StringComparer.OrdinalIgnoreCase) {
                        { item.Name, new [] { value } }
                    });
                };

                _valueReceivedHandlers[item.Id] = onValueReceived;

                item.SnapshotValueUpdated += onValueReceived;
                item.Deleted += UnsubscribeTag;
            }
        }


        /// <summary>
        /// Removes tags from the subscription.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The tags to subscribe to.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will remove the tag subscriptions.
        /// </returns>
        public async Task RemoveTags(ClaimsIdentity identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            var tags = await _historian.GetTags(identity, tagNames, cancellationToken).ConfigureAwait(false);
            foreach (var item in tags) {
                UnsubscribeTag(item);
            }
        }


        /// <summary>
        /// Unsubscribes from a tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        private void UnsubscribeTag(TagDefinition tag) {
            _subscribedTags.TryRemove(tag.Id, out var _);
            if (_valueReceivedHandlers.TryRemove(tag.Id, out var handler)) {
                tag.SnapshotValueUpdated -= handler;
            }
        }


        /// <summary>
        /// Unsubscribes from all tags.
        /// </summary>
        public void Dispose() {
            if (_isDisposed) {
                return;
            }

            _isDisposed = true;

            foreach (var item in _subscribedTags.Values.ToArray()) {
                UnsubscribeTag(item);
            }
        }
    }
}