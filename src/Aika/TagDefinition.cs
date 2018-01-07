using System;
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
        /// Gets the UTC creation time for the tag.
        /// </summary>
        public DateTime UtcCreatedAt { get; private set; }

        /// <summary>
        /// Gets the UTC last-modified time for the tag.
        /// </summary>
        public DateTime UtcLastModifiedAt { get; private set; }

        /// <summary>
        /// The snapshot value of the tag.
        /// </summary>
        private TagValue _snapshotValue;

        /// <summary>
        /// Gets the instantaneous snapshot value of the tag.
        /// </summary>
        public TagValue SnapshotValue {
            get { return _snapshotValue; }
            private set {
                _snapshotValue = value;
                SnapshotValueUpdated?.Invoke(value);
            }
        }

        /// <summary>
        /// Gets the data filter for the tag.
        /// </summary>
        public DataFilter DataFilter { get; }

        /// <summary>
        /// Raised whenever the snapshot value of the tag changes.
        /// </summary>
        public event Action<TagValue> SnapshotValueUpdated;


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
        /// <param name="utcCreatedAt">The UTC creation time for the tag.</param>
        /// <param name="utcLastModifiedAt">The UTC last-modified time for the tag.</param>
        /// <param name="snapshotValue">The tag's current snapshot value.  Can be <see langword="null"/>.</param>
        /// <param name="lastArchivedValue">The tag's last-archived value.  Can be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="historian"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
        /// <exception cref="ValidationException"><paramref name="settings"/> is not valid.</exception>
        protected TagDefinition(HistorianBase historian, string id, TagSettings settings, DateTime? utcCreatedAt, DateTime? utcLastModifiedAt, TagValue snapshotValue, TagValue lastArchivedValue) {
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
            UtcCreatedAt = utcCreatedAt ?? DateTime.UtcNow;
            UtcLastModifiedAt = utcLastModifiedAt ?? UtcCreatedAt;

            SnapshotValue = snapshotValue;
            DataFilter = new DataFilter(Name, new ExceptionFilterState(exceptionFilterSettings, SnapshotValue), new CompressionFilterState(compressionFilterSettings, lastArchivedValue, null), _historian.LoggerFactory);
            DataFilter.Archive += values => {
                if (_logger.IsEnabled(LogLevel.Trace)) {
                    _logger.LogTrace($"[{Name}] Archiving {values.Count()} values emitted by the compression filter.");
                }

                _historian.TaskRunner.RunBackgroundTask(async ct => {
                    try {
                        await InsertArchiveValues(ClaimsPrincipal.Current, values.ToArray(), ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) {
                        // App is shutting down...
                    }
                    catch (Exception e) {
                        _logger?.LogError($"[{Name}] An error occurred while archiving values emitted by the compression filter.", e);
                    }
                });
            };

            SnapshotValueUpdated += val => DataFilter.ValueReceived(val);
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
                TagValue valueThisIteration;
                if (stateSet == null) {
                    valueThisIteration = val;
                }
                else {
                    TagValue ssVal;

                    if (!TagValue.TryCreateFromStateSet(stateSet, val, out ssVal)) {
                        ++invalidSampleCount;
                        continue;
                    }

                    valueThisIteration = ssVal;
                }

                SnapshotValue = valueThisIteration;
                ++sampleCount;
                if (!earliestSampleTime.HasValue) {
                    earliestSampleTime = valueThisIteration.UtcSampleTime;
                }
                if (!latestSampleTime.HasValue || valueThisIteration.UtcSampleTime > latestSampleTime.Value) {
                    latestSampleTime = valueThisIteration.UtcSampleTime;
                }
            }

            return sampleCount == 0
                ? invalidSampleCount > 0 
                    ? new WriteTagValuesResult(false, 0, null, null, new[] { String.Format(CultureInfo.CurrentCulture, Resources.WriteTagValuesResult_InvalidValuesSpecified, invalidSampleCount) })
                    : new WriteTagValuesResult(false, 0, null, null, new[] { Resources.WriteTagValuesResult_NoValuesSpecified })
                : new WriteTagValuesResult(true, sampleCount, earliestSampleTime, latestSampleTime, invalidSampleCount == 0 ? null : new[] { String.Format(CultureInfo.CurrentCulture, Resources.WriteTagValuesResult_InvalidValuesSpecified, invalidSampleCount) });
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
            var stateSet = await GetStateSet(identity, cancellationToken).ConfigureAwait(false);

            if (stateSet != null) {
                var vals = new List<TagValue>();
                foreach (var value in values) {
                    if (!TagValue.TryCreateFromStateSet(stateSet, value, out var val)) {
                        continue;
                    }

                    vals.Add(val);
                }

                values = vals.ToArray();
            }

            try {
                return await OnInsertArchiveValues(values, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) {
                return new WriteTagValuesResult(false, 0, null, null, new[] { e.Message });
            }
        }


        /// <summary>
        /// When implemented in a derived class, inserts values into the historian archive.
        /// </summary>
        /// <param name="values">The values to insert.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The result of the insert.
        /// </returns>
        protected abstract Task<WriteTagValuesResult> OnInsertArchiveValues(IEnumerable<TagValue> values, CancellationToken cancellationToken);


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
        /// <exception cref="ArgumentNullException"><paramref name="update"/> is <see langword="null"/>.</exception>
        public virtual void Update(TagSettings update) {
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

            Updated?.Invoke(this);
        }


        /// <summary>
        /// Raises the <see cref="Deleted"/> event.
        /// </summary>
        protected void OnDeleted() {
            Deleted?.Invoke(this);
        }

    }
}
