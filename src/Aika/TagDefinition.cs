using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aika {
    /// <summary>
    /// Describes a tag in the back-end historian.
    /// </summary>
    public abstract class TagDefinition {

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The owning historian.
        /// </summary>
        private readonly HistorianBase _historian;

        /// <summary>
        /// Ensures that only one archive insert can occur at a time.
        /// </summary>
        private int _archiveLock;

        /// <summary>
        /// Holds pending archive writes.
        /// </summary>
        private readonly ConcurrentQueue<PendingArchiveWrite> _pendingArchiveWrites = new ConcurrentQueue<PendingArchiveWrite>();

        /// <summary>
        /// Gets the unique identifier for the tag.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the tag name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the tag description.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets the tag units.
        /// </summary>
        public string Units { get; private set; }

        /// <summary>
        /// Gets the tag's data type.
        /// </summary>
        public TagDataType DataType { get; private set; }

        /// <summary>
        /// Gets the name of the <see cref="StateSet"/> that the tag uses, when the <see cref="DataType"/> 
        /// is <see cref="TagDataType.State"/>.
        /// </summary>
        public string StateSet { get; private set; }

        /// <summary>
        /// Gets the change history for the tag.
        /// </summary>
        private readonly List<TagChangeHistoryEntry> _changeHistory = new List<TagChangeHistoryEntry>();

        /// <summary>
        /// Gets the change history for the tag.
        /// </summary>
        public IEnumerable<TagChangeHistoryEntry> ChangeHistory { get { return _changeHistory.ToArray(); } }

        /// <summary>
        /// The snapshot value of the tag.
        /// </summary>
        private TagValue _snapshotValue;

        /// <summary>
        /// Gets the instantaneous snapshot value of the tag.
        /// </summary>
        public TagValue SnapshotValue {
            get { return _snapshotValue; }
        }

        /// <summary>
        /// Gets the data filter for the tag.
        /// </summary>
        public DataFilter DataFilter { get; }

        /// <summary>
        /// Holds snapshot subscribers.
        /// </summary>
        private readonly ConcurrentDictionary<SnapshotValueSubscription, object> _snapshotSubscriptions = new ConcurrentDictionary<SnapshotValueSubscription, object>();

        /// <summary>
        /// Gets a flag that indicates if the tag currently has any snapshot subscribers.
        /// </summary>
        public bool HasSnapshotSupscriptions {
            get { return _snapshotSubscriptions.Count > 0; }
        }


        /// <summary>
        /// Raised whenever a snapshot subscription is added to or removed from the tag.
        /// </summary>
        public event Action<TagDefinition> SnapshotValueSubscriptionChange;


        /// <summary>
        /// Raised whenever the tag definition is updated.
        /// </summary>
        public event Action<TagDefinition> Updated;


        /// <summary>
        /// Raised when the tag definition is deleted.
        /// </summary>
        public event Action<TagDefinition> Deleted;


        /// <summary>
        /// Creates a new <see cref="TagDefinition"/> object.
        /// </summary>
        /// <param name="historian">The <see cref="IHistorian"/> instance that the tag belongs to.</param>
        /// <param name="id">The tag ID.  If <see langword="null"/>, a new tag ID will ge generated automatically.</param>
        /// <param name="settings">The tag settings.</param>
        /// <param name="initialTagValues">The initial values to configure the tag's exception and compression filters with.</param>
        /// <param name="changeHistory">The change history for the tag.</param>
        /// <exception cref="ArgumentNullException"><paramref name="historian"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
        /// <exception cref="ValidationException"><paramref name="settings"/> is not valid.</exception>
        protected TagDefinition(HistorianBase historian, string id, TagSettings settings, InitialTagValues initialTagValues, IEnumerable<TagChangeHistoryEntry> changeHistory) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
            _logger = _historian.LoggerFactory?.CreateLogger<TagDefinition>();

            if (settings == null) {
                throw new ArgumentNullException(nameof(settings));
            }

            Validator.ValidateObject(settings, new ValidationContext(settings), true);

            Id = id ?? CreateTagId();
            Name = settings.Name;
            Description = settings.Description;
            Units = settings.Units;
            DataType = settings.DataType;
            StateSet = settings.StateSet;
            var exceptionFilterSettings = settings.ExceptionFilterSettings?.ToTagValueFilterSettings() ?? new TagValueFilterSettings(false, TagValueFilterDeviationType.Absolute, 0, TimeSpan.FromDays(1));
            var compressionFilterSettings = settings.CompressionFilterSettings?.ToTagValueFilterSettings() ?? new TagValueFilterSettings(false, TagValueFilterDeviationType.Absolute, 0, TimeSpan.FromDays(1));
            
            if (changeHistory != null) {
                _changeHistory.AddRange(changeHistory);
            }

            _snapshotValue = initialTagValues?.SnapshotValue;
            DataFilter = new DataFilter(Name, new ExceptionFilterState(exceptionFilterSettings, _snapshotValue), new CompressionFilterState(compressionFilterSettings, initialTagValues?.LastArchivedValue, initialTagValues?.NextArchiveCandidateValue), _historian.LoggerFactory);
            DataFilter.Emit += (values, nextArchiveCandidate) => {
                if (values.Length > 0 && _logger.IsEnabled(LogLevel.Trace)) {
                    _logger.LogTrace($"[{Name}] Archiving {values.Count()} values emitted by the compression filter.");
                }

                _pendingArchiveWrites.Enqueue(new PendingArchiveWrite() { ArchiveValues = values, NextArchiveCandidate = nextArchiveCandidate });

                _historian.TaskRunner.RunBackgroundTask(async ct => {
                    if (Interlocked.CompareExchange(ref _archiveLock, 1, 0) != 0) {
                        return;
                    }

                    try {
                        while (!ct.IsCancellationRequested && _pendingArchiveWrites.TryDequeue(out var item)) {
                            await InsertArchiveValuesInternal(ClaimsPrincipal.Current, item.ArchiveValues, item.NextArchiveCandidate, false, ct).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException) {
                        // App is shutting down...
                    }
                    catch (Exception e) {
                        _logger?.LogError($"[{Name}] An error occurred while archiving values emitted by the compression filter.", e);
                    }
                    finally {
                        _archiveLock = 0;
                    }
                });
            };
        }


        /// <summary>
        /// Gets bespoke properties for the tag.  The default implementation of this method returns an empty dictionary.
        /// </summary>
        /// <returns>
        /// A dictionary containing custom properties associated with the tag.
        /// </returns>
        public virtual IDictionary<string, object> GetProperties() {
            return new Dictionary<string, object>();
        }


        /// <summary>
        /// Gets the state set object for the tag.
        /// </summary>
        /// <param name="identity">The identity of the calling user.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the state set object.
        /// </returns>
        private async Task<StateSet> GetStateSet(ClaimsPrincipal identity, CancellationToken cancellationToken) {
            if (DataType != TagDataType.State) {
                return null;
            }

            if (String.IsNullOrWhiteSpace(StateSet)) {
                throw new InvalidOperationException(Resources.Error_StateSetNameIsRequired);
            }

            return await _historian.GetStateSet(identity, StateSet, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Creates a new snapshot value subscription.
        /// </summary>
        /// <param name="callback">The callback function to invoke when the value changes.</param>
        /// <returns>
        /// An <see cref="IDisposable"/> that represents the subscription.  Dispose the object to unsubscribe.
        /// </returns>
        internal IDisposable CreateSnapshotSubscription(Action<TagValue> callback) {
            return new SnapshotValueSubscription(this, callback);
        }


        /// <summary>
        /// Writes new snapshot values to the tag.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="values">
        ///   The values to write.  Values that are more recent than the current snapshot value of the 
        ///   tag will be passed into the tag's data filter to test if they should be forwarded to the 
        ///   historian archive.
        /// </param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the write result.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        public async Task<WriteTagValuesResult> WriteSnapshotValues(ClaimsPrincipal identity, IEnumerable<TagValue> values, CancellationToken cancellationToken) {
            var stateSet = await GetStateSet(identity, cancellationToken).ConfigureAwait(false);

            var currentSnapshot = SnapshotValue;

            DateTime? earliestSampleTime = null;
            DateTime? latestSampleTime = null;

            var sampleCount = 0;
            var invalidSampleCount = 0;

            var valsToWrite = values == null
                ? (IEnumerable<TagValue>) new TagValue[0]
                : values.Where(x => x != null && (currentSnapshot == null || x.UtcSampleTime > currentSnapshot.UtcSampleTime))
                        .OrderBy(x => x.UtcSampleTime);

            foreach (var val in valsToWrite) {
                if (!this.TryValidateIncomingTagValue(val, stateSet, out var validatedValue)) {
                    ++invalidSampleCount;
                    continue;
                }

                UpdateSnapshotValue(validatedValue);
                ++sampleCount;
                if (!earliestSampleTime.HasValue) {
                    earliestSampleTime = validatedValue.UtcSampleTime;
                }
                if (!latestSampleTime.HasValue || validatedValue.UtcSampleTime > latestSampleTime.Value) {
                    latestSampleTime = validatedValue.UtcSampleTime;
                }
            }

            return sampleCount == 0
                ? invalidSampleCount > 0 
                    ? new WriteTagValuesResult(false, 0, null, null, new[] { String.Format(CultureInfo.CurrentCulture, Resources.WriteTagValuesResult_InvalidValuesSpecified, invalidSampleCount) })
                    : new WriteTagValuesResult(false, 0, null, null, new[] { Resources.WriteTagValuesResult_NoValuesSpecified })
                : new WriteTagValuesResult(true, sampleCount, earliestSampleTime, latestSampleTime, invalidSampleCount == 0 ? null : new[] { String.Format(CultureInfo.CurrentCulture, Resources.WriteTagValuesResult_InvalidValuesSpecified, invalidSampleCount) });
        }


        /// <summary>
        /// Inserts values into the historian archive, optionally skipping value validation (e.g. if 
        /// the insert is because of a value emitted from the compression filter).
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="values">The values to insert.</param>
        /// <param name="nextArchiveCandidate">The current candidate for the next value to be inserted.</param>
        /// <param name="validate">Flags if the values should be validated before sending to the archive.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the write result.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        private async Task<WriteTagValuesResult> InsertArchiveValuesInternal(ClaimsPrincipal identity, IEnumerable<TagValue> values, TagValue nextArchiveCandidate, bool validate, CancellationToken cancellationToken) {
            if (!values?.Any() ?? false) {
                await InsertArchiveValues(null, nextArchiveCandidate, cancellationToken).ConfigureAwait(false);
                return WriteTagValuesResult.CreateEmptyResult();
            }

            var stateSet = await GetStateSet(identity, cancellationToken).ConfigureAwait(false);

            if (validate) {
                var vals = new List<TagValue>();
                foreach (var value in values) {
                    if (validate) {
                        if (!this.TryValidateIncomingTagValue(value, stateSet, out var validatedValue)) {
                            continue;
                        }

                        vals.Add(validatedValue);
                    }
                }

                values = vals.ToArray();
            }

            try {
                return await InsertArchiveValues(values, nextArchiveCandidate, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) {
                return new WriteTagValuesResult(false, 0, null, null, new[] { e.Message });
            }
        }


        /// <summary>
        /// Inserts values into the historian archive.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="values">The values to insert.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the write result.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        public async Task<WriteTagValuesResult> InsertArchiveValues(ClaimsPrincipal identity, IEnumerable<TagValue> values, CancellationToken cancellationToken) {
            return await InsertArchiveValuesInternal(identity, values, DataFilter.CompressionFilter.LastReceivedValue, true, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// When implemented in a derived type, saves a new snapshot value to the back-end historian.
        /// </summary>
        /// <param name="value">The new snapshot value for the tag.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will save the new snapshot value.
        /// </returns>
        protected abstract Task SaveSnapshotValue(TagValue value, CancellationToken cancellationToken);


        /// <summary>
        /// When implemented in a derived class, inserts values into the historian archive.
        /// </summary>
        /// <param name="values">The values to insert.  Can be <see langword="null"/> or empty if only the <paramref name="nextArchiveCandidate"/> is being updated.</param>
        /// <param name="nextArchiveCandidate">The candidate for the next value to write to the archive.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The result of the insert.
        /// </returns>
        protected abstract Task<WriteTagValuesResult> InsertArchiveValues(IEnumerable<TagValue> values, TagValue nextArchiveCandidate, CancellationToken cancellationToken);


        /// <summary>
        /// Updates the snapshot value for the tag.  You do not need to call this method from your 
        /// implementation unless you have disabled writing of new tag values from Aika (e.g. if 
        /// you are integrating with an existing historian and do not want to allow writes via Aika).
        /// </summary>
        /// <param name="value">The updated value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        protected void UpdateSnapshotValue(TagValue value) {
            _snapshotValue = value ?? throw new ArgumentNullException(nameof(value));
            DataFilter.ValueReceived(value);
            _historian.TaskRunner.RunBackgroundTask(ct => SaveSnapshotValue(value, ct));
            foreach (var subscriber in _snapshotSubscriptions.Keys) {
                try {
                    subscriber.OnValueChange(value);
                }
                catch (Exception e) {
                    _logger?.LogError("An error occurred while notifying a snapshot subscriber of a change.", e);
                }
            }
        }

        /// <summary>
        /// Creates a new tag identifier.
        /// </summary>
        /// <returns>A unique identifier for a tag.</returns>
        private static string CreateTagId() {
            return Guid.NewGuid().ToString();
        }


        /// <summary>
        /// Updates the tag.
        /// </summary>
        /// <param name="update">The updated tag settings.</param>
        /// <param name="modifier">The tag's modifier.</param>
        /// <param name="description">The description of the update.</param>
        /// <returns>
        /// The new change history entry for the tag.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="update"/> is <see langword="null"/>.</exception>
        public virtual TagChangeHistoryEntry Update(TagSettings update, ClaimsPrincipal modifier, string description) {
            if (update == null) {
                throw new ArgumentNullException(nameof(update));
            }

            if (!String.IsNullOrWhiteSpace(update.Name)) {
                Name = update.Name;
            }
            if (update.Description != null) {
                Description = update.Description;
            }
            if (update.Units != null) {
                Units = update.Units;
            }

            DataType = update.DataType;
            StateSet = update.StateSet;

            if (update.ExceptionFilterSettings != null) {
                DataFilter.ExceptionFilter.Settings.Update(update.ExceptionFilterSettings);
            }
            if (update.CompressionFilterSettings != null) {
                DataFilter.CompressionFilter.Settings.Update(update.CompressionFilterSettings);
            }

            var historyItem = TagChangeHistoryEntry.Updated(modifier, description);
            _changeHistory.Add(historyItem);

            Updated?.Invoke(this);

            return historyItem;
        }


        /// <summary>
        /// Raises the <see cref="Deleted"/> event.
        /// </summary>
        protected void OnDeleted() {
            Deleted?.Invoke(this);
        }


        /// <summary>
        /// Describes a pending write to the historian.
        /// </summary>
        private class PendingArchiveWrite {

            /// <summary>
            /// The values to permanently write to the archive.
            /// </summary>
            public TagValue[] ArchiveValues { get; set; }

            /// <summary>
            /// The value to write as the next archive candidate.
            /// </summary>
            public TagValue NextArchiveCandidate { get; set; }

        }


        /// <summary>
        /// Describes a snapshot subscriber on a tag.
        /// </summary>
        private class SnapshotValueSubscription : IDisposable {

            /// <summary>
            /// The tag that the subscription is for.
            /// </summary>
            private readonly TagDefinition _tag;

            /// <summary>
            /// The delegate to invoke when the snapshot value of the tag changes.
            /// </summary>
            private readonly Action<TagValue> _onValueChange;


            /// <summary>
            /// Creates a new <see cref="SnapshotValueSubscription"/> object.
            /// </summary>
            /// <param name="tag">The tag.</param>
            /// <param name="onValueChange">The value change callback.</param>
            internal SnapshotValueSubscription(TagDefinition tag, Action<TagValue> onValueChange) {
                _tag = tag ?? throw new ArgumentNullException(nameof(tag));
                _onValueChange = onValueChange ?? throw new ArgumentNullException(nameof(onValueChange));

                _tag._snapshotSubscriptions[this] = new object();
                _tag.SnapshotValueSubscriptionChange?.Invoke(_tag);
            }


            /// <summary>
            /// Sends a value change to the subscriber.
            /// </summary>
            /// <param name="value">The new value.</param>
            internal void OnValueChange(TagValue value) {
                _onValueChange?.Invoke(value);
            }


            /// <summary>
            /// Disposes of the subscription.
            /// </summary>
            public void Dispose() {
                if (_tag._snapshotSubscriptions.TryRemove(this, out var removed)) {
                    _tag.SnapshotValueSubscriptionChange?.Invoke(_tag);
                }
            }

        }

    }

}
