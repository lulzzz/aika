using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika.Tags;

namespace Aika {
    /// <summary>
    /// Describes a real-time subscription that receives snapshot value changes as they occur.
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
        /// Holds the subscriptions for each subscribed tag.
        /// </summary>
        private readonly ConcurrentDictionary<string, IDisposable> _subscriptions = new ConcurrentDictionary<string, IDisposable>(StringComparer.OrdinalIgnoreCase);

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
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tagNames"/> does not contain any non-null-or-empty entries.</exception>
        public async Task AddTags(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            var distinctTagNames = tagNames?.Where(x => String.IsNullOrWhiteSpace(x))
                                            .Select(x => x.Trim())
                                            .Distinct(StringComparer.OrdinalIgnoreCase)
                                            .ToArray();
            if (distinctTagNames?.Length == 0) {
                throw new ArgumentException(Resources.Error_AtLeastOneTagNameRequired, nameof(tagNames));
            }

            var tags = await _historian.GetTags(identity, distinctTagNames, cancellationToken).ConfigureAwait(false);
            foreach (var item in tags.Values) {
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

                _subscriptions[item.Id] = item.CreateSnapshotSubscription(onValueReceived);

                if (item.SnapshotValue != null) {
                    onValueReceived.Invoke(item.SnapshotValue);
                }

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
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tagNames"/> does not contain any non-null-or-empty entries.</exception>
        public async Task RemoveTags(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            var distinctTagNames = tagNames?.Where(x => String.IsNullOrWhiteSpace(x))
                                            .Select(x => x.Trim())
                                            .Distinct(StringComparer.OrdinalIgnoreCase)
                                            .ToArray();
            if (distinctTagNames?.Length == 0) {
                throw new ArgumentException(Resources.Error_AtLeastOneTagNameRequired, nameof(tagNames));
            }

            var tags = await _historian.GetTags(identity, distinctTagNames, cancellationToken).ConfigureAwait(false);
            foreach (var item in tags.Values) {
                UnsubscribeTag(item);
            }
        }


        /// <summary>
        /// Unsubscribes from a tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        private void UnsubscribeTag(TagDefinition tag) {
            _subscribedTags.TryRemove(tag.Id, out var _);
            if (_subscriptions.TryRemove(tag.Id, out var subscription)) {
                subscription.Dispose();
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
