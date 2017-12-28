using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IHistorian _historian;

        /// <summary>
        /// The task runner the run background tasks with.
        /// </summary>
        private readonly ITaskRunner _taskRunner;

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
        /// <param name="taskRunner">The <see cref="ITaskRunner"/> instance to run background tasks with.</param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="units"></param>
        /// <param name="dataType"></param>
        /// <param name="stateSet"></param>
        /// <param name="exceptionFilterSettings"></param>
        /// <param name="compressionFilterSettings"></param>
        /// <param name="utcCreatedAt"></param>
        /// <param name="utcLastModifiedAt"></param>
        /// <param name="snapshotValue"></param>
        /// <param name="lastArchivedValue"></param>
        /// <param name="loggerFactory"></param>
        /// <exception cref="ArgumentNullException"><paramref name="historian"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="taskRunner"/> is <see langword="null"/>.</exception>
        protected TagDefinition(IHistorian historian, ITaskRunner taskRunner, string id, string name, string description, string units, TagDataType dataType, string stateSet, TagValueFilterSettings exceptionFilterSettings, TagValueFilterSettings compressionFilterSettings, DateTime? utcCreatedAt, DateTime? utcLastModifiedAt, TagValue snapshotValue, TagValue lastArchivedValue, ILoggerFactory loggerFactory) {
            _logger = loggerFactory?.CreateLogger<TagDefinition>();
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
            _taskRunner = taskRunner ?? throw new ArgumentNullException(nameof(taskRunner));

            Id = id ?? CreateTagId();
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            Units = units;
            DataType = dataType;
            StateSet = stateSet;
            exceptionFilterSettings = exceptionFilterSettings ?? new TagValueFilterSettings(false, TagValueFilterDeviationType.Absolute, 0, TimeSpan.FromDays(1));
            compressionFilterSettings = compressionFilterSettings ?? new TagValueFilterSettings(false, TagValueFilterDeviationType.Absolute, 0, TimeSpan.FromDays(1));
            UtcCreatedAt = utcCreatedAt ?? DateTime.UtcNow;
            UtcLastModifiedAt = utcLastModifiedAt ?? UtcCreatedAt;

            SnapshotValue = snapshotValue;
            DataFilter = new DataFilter(Name, new ExceptionFilterState(exceptionFilterSettings, SnapshotValue), new CompressionFilterState(compressionFilterSettings, lastArchivedValue, null), loggerFactory);
            DataFilter.Archive += values => {
                if (_logger.IsEnabled(LogLevel.Trace)) {
                    _logger.LogTrace($"[{Name}] Archiving {values.Count()} values emitted by the compression filter.");
                }

                _taskRunner.RunBackgroundTask(async ct => {
                    try {
                        await InsertArchiveValues(values.ToArray(), ct).ConfigureAwait(false);
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


        /// <summary>
        /// When implemented in a dervived class, gets the last-archived value for the tag.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return either the last-archived value for the tag, or <see langword="null"/> 
        /// if the last-archived value is unavailable or undefined.
        /// </returns>
        public abstract Task<TagValue> GetLastArchivedValue(CancellationToken cancellationToken);


        /// <summary>
        /// Writes new snapshot values to the tag.
        /// </summary>
        /// <param name="values">
        ///   The values to write.  Values that are more recent than the current snapshot value of the 
        ///   tag will be passed into the tag's data filter to test if they should be forwarded to the 
        ///   historian archive.
        /// </param>
        public WriteTagValuesResult WriteSnapshotValues(IEnumerable<TagValue> values) {
            var currentSnapshot = SnapshotValue;

            DateTime? earliestSampleTime = null;
            DateTime? latestSampleTime = null;

            var sampleCount = 0;
            var valsToWrite = values == null
                ? (IEnumerable<TagValue>) new TagValue[0]
                : values.Where(x => x != null && (currentSnapshot == null || x.UtcSampleTime > currentSnapshot.UtcSampleTime)).OrderBy(x => x.UtcSampleTime);

            foreach (var val in valsToWrite) {
                SnapshotValue = val;
                ++sampleCount;
                if (!earliestSampleTime.HasValue) {
                    earliestSampleTime = val.UtcSampleTime;
                }
                if (!latestSampleTime.HasValue || val.UtcSampleTime > latestSampleTime.Value) {
                    latestSampleTime = val.UtcSampleTime;
                }
            }

            return sampleCount == 0
                ? new WriteTagValuesResult(false, 0, null, null, new[] { "No values specified." })
                : new WriteTagValuesResult(true, sampleCount, earliestSampleTime, latestSampleTime, null);
        }


        /// <summary>
        /// When implemented in a derived class, inserts values into the historian archive.
        /// </summary>
        /// <param name="values">The values to insert.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns></returns>
        public async Task<WriteTagValuesResult> InsertArchiveValues(IEnumerable<TagValue> values, CancellationToken cancellationToken) {
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
        public static string CreateTagId() {
            return Guid.NewGuid().ToString();
        }


        /// <summary>
        /// Updates the tag.
        /// </summary>
        /// <param name="update">The updated tag settings.</param>
        /// <exception cref="ArgumentNullException"><paramref name="update"/> is <see langword="null"/>.</exception>
        public virtual void Update(TagDefinitionUpdate update) {
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
