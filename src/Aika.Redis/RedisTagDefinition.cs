using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika.Tags;
using Aika.Tags.Security;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Aika.Redis {
    /// <summary>
    /// Tag definition for a <see cref="RedisHistorian"/>.
    /// </summary>
    public class RedisTagDefinition : TagDefinition {

        /// <summary>
        /// The historian that owns the tag.
        /// </summary>
        private readonly RedisHistorian _historian;

        /// <summary>
        /// The Redis key that holds the tag definition.
        /// </summary>
        private readonly string _tagDefinitionKey;

        /// <summary>
        /// The Redis key that holds the current snapshot value.
        /// </summary>
        private readonly string _snapshotKey;

        /// <summary>
        /// The Redis key that holds the current archive candidate value (i.e. the value that would be 
        /// archived if the next incoming value passes the tag's compression filter).
        /// </summary>
        private readonly string _archiveCandidateKey;

        /// <summary>
        /// The Redis key that raw data is archived to.
        /// </summary>
        private readonly string _archiveKey;

        /// <summary>
        /// Lock for writing snapshot updates to Redis.
        /// </summary>
        private int _snapshotUpdateLock;

        /// <summary>
        /// Queue of pending snapshot values to be written.
        /// </summary>
        private readonly ConcurrentQueue<TagValue> _pendingSnapshotWrites = new ConcurrentQueue<TagValue>();

        /// <summary>
        /// Holds the next archive candidate value for the tag i.e. the value that would be written 
        /// permanently to the archive if the next incoming value passed both the exception and 
        /// compression filter tests for the tag.
        /// </summary>
        private ArchiveCandidateValue _archiveCandidateValue;

        /// <summary>
        /// The maximum number of raw samples to retrieve per query.
        /// </summary>
        private const int MaximumRawSampleCount = 2000;


        /// <summary>
        /// Creates a new <see cref="RedisTagDefinition"/> object.
        /// </summary>
        /// <param name="historian">The owning historian.</param>
        /// <param name="id">The tag ID.</param>
        /// <param name="settings">The tag settings.</param>
        /// <param name="metadata">The metadata for the tag.</param>
        /// <param name="initialTagValues">The initial tag values, used to prime the exception and compression filters for the tag.</param>
        /// <param name="changeHistory">The change history for the tag.</param>
        private RedisTagDefinition(RedisHistorian historian, string id, TagSettings settings, TagMetadata metadata, InitialTagValues initialTagValues, IEnumerable<TagChangeHistoryEntry> changeHistory) : base(historian, id, settings, metadata, CreateTagSecurity(), initialTagValues, changeHistory) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
            _tagDefinitionKey = _historian.GetKeyForTagDefinition(Id);
            _snapshotKey = _historian.GetKeyForSnapshotData(Id);
            _archiveKey = _historian.GetKeyForRawData(Id);
            _archiveCandidateKey = _historian.GetKeyForArchiveCandidateData(Id);

            _archiveCandidateValue = new ArchiveCandidateValue(initialTagValues?.LastExceptionValue, initialTagValues?.CompressionAngleMinimum ?? Double.NaN, initialTagValues?.CompressionAngleMaximum ?? Double.NaN);

            Updated += TagUpdated;
        }


        private static TagSecurity CreateTagSecurity() {
            return new TagSecurity(null, new Dictionary<string, TagSecurityPolicy>() {
                {
                    TagSecurityPolicy.Administrator,
                    new TagSecurityPolicy(new [] { new TagSecurityEntry(null, "*") }, null)
                },
                {
                    TagSecurityPolicy.DataRead,
                    new TagSecurityPolicy(new [] { new TagSecurityEntry(null, "*") }, null)
                },
                {
                    TagSecurityPolicy.DataWrite,
                    new TagSecurityPolicy(new [] { new TagSecurityEntry(null, "*") }, null)
                }
            });
        }


        /// <summary>
        /// Saves a new snapshot value to Redis.
        /// </summary>
        /// <param name="value">The new snapshot value.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will enqueue the snapshot value to be saved.
        /// </returns>
        protected override Task SaveSnapshotValue(TagValue value, CancellationToken cancellationToken) {
            EnqueueSnapshotValueSave(this, value);
            return Task.CompletedTask;
        }


        /// <summary>
        /// Writes archive values to Redis.
        /// </summary>
        /// <param name="values">The values to write.</param>
        /// <param name="nextArchiveCandidate">The next archive candiate value.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the write result.
        /// </returns>
        protected override async Task<WriteTagValuesResult> InsertArchiveValues(IEnumerable<TagValue> values, ArchiveCandidateValue nextArchiveCandidate, CancellationToken cancellationToken) {
            await SaveArchiveCandidateValueInternal(nextArchiveCandidate, cancellationToken).ConfigureAwait(false);
            await SaveArchiveValues(values, cancellationToken).ConfigureAwait(false);
            return new WriteTagValuesResult(true, values?.Count() ?? 0, values?.FirstOrDefault()?.UtcSampleTime, values?.LastOrDefault()?.UtcSampleTime, null);
        }

        #region [ Tag Event Handlers ]

        /// <summary>
        /// Saves updated tag definitions to Redis.
        /// </summary>
        /// <param name="updated">The updated tag definition.</param>
        private static void TagUpdated(TagDefinition updated) {
            var redisTag = updated as RedisTagDefinition;
            if (redisTag == null) {
                return;
            }

            redisTag._historian.RunBackgroundTask(ct => redisTag.Save(ct));
        }


        /// <summary>
        /// Enqueues the specified snapshot value for writing to Redis.
        /// </summary>
        /// <param name="tag">The tag that the value belongs to.</param>
        /// <param name="value">The value.</param>
        private static void EnqueueSnapshotValueSave(RedisTagDefinition tag, TagValue value) {
            // Enqueue the value.
            tag._pendingSnapshotWrites.Enqueue(value);

            // If a write task is already in progress, we'll just exit now.
            if (Interlocked.CompareExchange(ref tag._snapshotUpdateLock, 1, 0) != 0) {
                return;
            }

            tag._historian.RunBackgroundTask(async ct => {
                try {
                    do {
                        while (tag._pendingSnapshotWrites.TryDequeue(out var val)) {
                            await tag.SaveSnapshotValueInternal(val, ct).ConfigureAwait(false);
                        }
                    } while (!tag._pendingSnapshotWrites.IsEmpty);
                }
                finally {
                    tag._snapshotUpdateLock = 0;
                }
            });
        }

        #endregion

        #region [ Load / Save / Create / Update / Delete Tag Definitions ]

        /// <summary>
        /// Creates a new <see cref="RedisTagDefinition"/>.
        /// </summary>
        /// <param name="historian">The historian for the tag.</param>
        /// <param name="settings">The tag settings.</param>
        /// <param name="creator">The tag's creator.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will create a new <see cref="RedisTagDefinition"/> and save it to the 
        /// historian's Redis server.
        /// </returns>
        internal static async Task<RedisTagDefinition> Create(RedisHistorian historian, TagSettings settings, ClaimsPrincipal creator, CancellationToken cancellationToken) {
            var now = DateTime.UtcNow;
            var result = new RedisTagDefinition(historian, null, settings, new TagMetadata(DateTime.UtcNow, creator?.Identity.Name), null, new[] { TagChangeHistoryEntry.Created(creator) });
            var key = historian.GetKeyForTagIdsList();
            
            await Task.WhenAny(Task.WhenAll(result.Save(cancellationToken), historian.Connection.GetDatabase().ListRightPushAsync(key, result.Id)), Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            return result;
        }


        /// <summary>
        /// Saves the tag definition to the Redis database.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will save the tag definition to the Redis database.
        /// </returns>
        internal async Task Save(CancellationToken cancellationToken) {
            var t = _historian.Connection
                              .GetDatabase()
                              .HashSetAsync(_tagDefinitionKey,
                                   new[] {
                                       new HashEntry("ID", Id),
                                       new HashEntry("NAME", Name),
                                       new HashEntry("DESC", Description ?? String.Empty),
                                       new HashEntry("UNITS", Units ?? String.Empty),
                                       new HashEntry("TYPE", (int) DataType),
                                       new HashEntry("SSET", StateSet ?? String.Empty),
                                       new HashEntry("EXC_ENABLED", Convert.ToInt32(DataFilter.ExceptionFilter.Settings.IsEnabled)),
                                       new HashEntry("EXC_LIMIT_TYPE", (int) DataFilter.ExceptionFilter.Settings.LimitType),
                                       new HashEntry("EXC_LIMIT", DataFilter.ExceptionFilter.Settings.Limit),
                                       new HashEntry("EXC_WINDOW", DataFilter.ExceptionFilter.Settings.WindowSize.ToString()),
                                       new HashEntry("COM_ENABLED", Convert.ToInt32(DataFilter.CompressionFilter.Settings.IsEnabled)),
                                       new HashEntry("COM_LIMIT_TYPE", (int) DataFilter.CompressionFilter.Settings.LimitType),
                                       new HashEntry("COM_LIMIT", DataFilter.CompressionFilter.Settings.Limit),
                                       new HashEntry("COM_WINDOW", DataFilter.CompressionFilter.Settings.WindowSize.ToString()),
                                       new HashEntry("MD_CREATEDAT", Metadata.UtcCreatedAt.Ticks),
                                       new HashEntry("MD_CREATOR", Metadata.Creator),
                                       new HashEntry("MD_MODIFIEDAT", Metadata.UtcLastModifiedAt.Ticks),
                                       new HashEntry("MD_MODIFIEDBY", Metadata.LastModifiedBy)
                                   });

            await Task.WhenAny(t, Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }


        /// <summary>
        /// Loads a tag definition from the Redis database.
        /// </summary>
        /// <param name="historian">The Redis historian to load the tag from.</param>
        /// <param name="tagId">The ID of the tag to load.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the loaded tag definition.
        /// </returns>
        internal static async Task<RedisTagDefinition> Load(RedisHistorian historian, string tagId, CancellationToken cancellationToken) {
            var values = await historian.Connection.GetDatabase().HashGetAllAsync(historian.GetKeyForTagDefinition(tagId)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            string name = null;
            string description = null;
            string units = null;
            TagDataType dataType = default(TagDataType);
            string stateSet = null;

            bool exceptionFilterEnabled = false;
            TagValueFilterDeviationType exceptionFilterLimitType = default(TagValueFilterDeviationType);
            double exceptionFilterLimit = 0;
            TimeSpan exceptionFilterWindowSize = default(TimeSpan);

            bool compressionFilterEnabled = false;
            TagValueFilterDeviationType compressionFilterLimitType = default(TagValueFilterDeviationType);
            double compressionFilterLimit = 0;
            TimeSpan compressionFilterWindowSize = default(TimeSpan);

            DateTime createdAt = DateTime.MinValue;
            string creator = null;
            DateTime modifiedAt = DateTime.MinValue;
            string modifiedBy = null;

            foreach (var item in values) {
                switch (item.Name.ToString()) {
                    case "NAME":
                        name = item.Value;
                        break;
                    case "DESC":
                        description = item.Value;
                        break;
                    case "UNITS":
                        units = item.Value;
                        break;
                    case "TYPE":
                        dataType = (TagDataType) ((int) item.Value);
                        break;
                    case "SSET":
                        stateSet = item.Value;
                        break;
                    case "EXC_ENABLED":
                        exceptionFilterEnabled = Convert.ToBoolean((int) item.Value);
                        break;
                    case "EXC_LIMIT_TYPE":
                        exceptionFilterLimitType = (TagValueFilterDeviationType) ((int) item.Value);
                        break;
                    case "EXC_LIMIT":
                        exceptionFilterLimit = (double) item.Value;
                        break;
                    case "EXC_WINDOW":
                        exceptionFilterWindowSize = TimeSpan.Parse(item.Value);
                        break;
                    case "COM_ENABLED":
                        compressionFilterEnabled = Convert.ToBoolean((int) item.Value);
                        break;
                    case "COM_LIMIT_TYPE":
                        compressionFilterLimitType = (TagValueFilterDeviationType) ((int) item.Value);
                        break;
                    case "COM_LIMIT":
                        compressionFilterLimit = (double) item.Value;
                        break;
                    case "COM_WINDOW":
                        compressionFilterWindowSize = TimeSpan.Parse(item.Value);
                        break;
                    case "MD_CREATEDAT":
                        createdAt = new DateTime((long) item.Value, DateTimeKind.Utc);
                        break;
                    case "MD_CREATEDBY":
                        creator = item.Value;
                        break;
                    case "MD_MODIFIEDAT":
                        modifiedAt = new DateTime((long) item.Value, DateTimeKind.Utc);
                        break;
                    case "MD_MODIFIEDBY":
                        modifiedBy = item.Value;
                        break;
                }
            }

            if (String.IsNullOrWhiteSpace(name)) {
                name = tagId;
            }

            var settings = new TagSettings() {
                Name = name,
                Description = description,
                Units = units,
                DataType = dataType,
                StateSet = stateSet,
                ExceptionFilterSettings = new TagValueFilterSettingsUpdate() {
                    IsEnabled = exceptionFilterEnabled,
                    LimitType = exceptionFilterLimitType,
                    Limit = exceptionFilterLimit,
                    WindowSize = exceptionFilterWindowSize
                },
                CompressionFilterSettings = new TagValueFilterSettingsUpdate() {
                    IsEnabled = compressionFilterEnabled,
                    LimitType = compressionFilterLimitType,
                    Limit = compressionFilterLimit,
                    WindowSize = compressionFilterWindowSize
                }
            };

            var metadata = new TagMetadata(createdAt, creator, modifiedAt, modifiedBy);

            var snapshotTask = LoadSnapshotValue(historian, tagId, cancellationToken);
            var lastArchivedTask = LoadLastArchivedValue(historian, tagId, cancellationToken);
            var archiveCandidateTask = LoadArchiveCandidateValue(historian, tagId, cancellationToken);

            await Task.WhenAll(snapshotTask, lastArchivedTask, archiveCandidateTask).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            var initialValues = new InitialTagValues(snapshotTask.Result, lastArchivedTask.Result, archiveCandidateTask.Result.Value, archiveCandidateTask.Result.CompressionAngleMinimum, archiveCandidateTask.Result.CompressionAngleMaximum);

            var result = new RedisTagDefinition(historian,
                                                tagId,
                                                settings,
                                                metadata,
                                                initialValues,
                                                null);

            return result;
        }


        /// <summary>
        /// Loads all tag definitions for the specified historian.
        /// </summary>
        /// <param name="historian">The historian.</param>
        /// <param name="callback">A callback function that is invoked every time a tag definition is loaded from Redis.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will load all tags into the historian.
        /// </returns>
        internal static async Task LoadAll(RedisHistorian historian, Action<RedisTagDefinition> callback, CancellationToken cancellationToken) {
            var key = historian.GetKeyForTagIdsList();
            
            const int pageSize = 100;
            var page = 0;
            bool @continue;

            // Load tag definitions 100 at a time.
            do {
                @continue = false;
                ++page;

                long start = (page - 1) * pageSize;
                long end = start + pageSize - 1; // -1 because right-most item is included when getting the list range.

                var tagIds = await historian.Connection.GetDatabase().ListRangeAsync(key, start, end).ConfigureAwait(false);
                @continue = tagIds.Length == pageSize;
                if (tagIds.Length == 0) {
                    continue;
                }

                var tasks = tagIds.Select(x => Task.Run(async () => {
                    var tag = await Load(historian, x, cancellationToken).ConfigureAwait(false);
                    callback(tag);
                })).ToArray();

                await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            } while (@continue);
        }


        /// <summary>
        /// Deletes the tag definition and raises the <see cref="TagDefinition.Deleted"/> event.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will delete the tag.
        /// </returns>
        internal async Task Delete(CancellationToken cancellationToken) {
            var tasks = new List<Task>();
            var db = _historian.Connection.GetDatabase();

            // Delete the tag definition.
            tasks.Add(db.KeyDeleteAsync(_tagDefinitionKey));

            // Delete the snapshot value.
            tasks.Add(db.KeyDeleteAsync(_snapshotKey));

            // Delete the raw values.
            tasks.Add(db.KeyDeleteAsync(_archiveKey));

            // Delete the archive candidate value.
            tasks.Add(db.KeyDeleteAsync(_archiveCandidateKey));

            // Remove the tag ID from the list of defined tag IDs.
            var tagIdListKey = _historian.GetKeyForTagIdsList();
            tasks.Add(_historian.Connection.GetDatabase().ListRemoveAsync(tagIdListKey, Id));

            try {
                await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
            finally {
                Updated -= TagUpdated;
                OnDeleted();
            }
        }

        #endregion

        #region [ Load / Save Tag Values ]

        /// <summary>
        /// Converts a <see cref="TagValue"/> into a set of <see cref="HashEntry"/> objects to write to Redis.
        /// </summary>
        /// <param name="value">The tag value.</param>
        /// <returns>
        /// The equivalent Redis hash entries.
        /// </returns>
        private HashEntry[] GetHashEntriesForTagValue(TagValue value) {
            if (value == null) {
                return new HashEntry[0];
            }

            return new[] {
                new HashEntry("TS", value.UtcSampleTime.Ticks),
                new HashEntry("N", value.NumericValue),
                new HashEntry("T", value.TextValue),
                new HashEntry("Q", (int) value.Quality),
                new HashEntry("U", Units ?? String.Empty),
            };
        }


        /// <summary>
        /// Converts an <see cref="ArchiveCandidateValue"/> into a set of <see cref="HashEntry"/> objects to write to Redis.
        /// </summary>
        /// <param name="value">The archive candidate tag value.</param>
        /// <returns>
        /// The equivalent Redis hash entries.
        /// </returns>
        private HashEntry[] GetHashEntriesForArchiveCandidateValue(ArchiveCandidateValue value) {
            if (value == null) {
                return new HashEntry[0];
            }

            var tagValueHashEntries = GetHashEntriesForTagValue(value.Value);
            return tagValueHashEntries.Concat(new[] {
                new HashEntry("MI", value.CompressionAngleMinimum),
                new HashEntry("MA", value.CompressionAngleMaximum)
            }).ToArray();
        }


        /// <summary>
        /// Converts a set of Redis <see cref="HashEntry"/> objects back into a <see cref="TagValue"/>.
        /// </summary>
        /// <param name="values">The Redis hash entries containing the tag value details.</param>
        /// <returns>
        /// The equivalent tag value.
        /// </returns>
        private static TagValue GetTagValueFromHashValues(HashEntry[] values) {
            DateTime utcSampleTime = DateTime.MinValue;
            double numericValue = Double.NaN;
            string textValue = null;
            TagValueQuality quality = TagValueQuality.Uncertain;
            string units = null;

            foreach (var item in values) {
                switch (item.Name.ToString()) {
                    case "TS":
                        utcSampleTime = new DateTime((long) item.Value, DateTimeKind.Utc);
                        break;
                    case "N":
                        numericValue = (double) item.Value;
                        break;
                    case "T":
                        textValue = (string) item.Value;
                        break;
                    case "Q":
                        quality = (TagValueQuality) ((int) item.Value);
                        break;
                    case "U":
                        units = (string) item.Value;
                        break;
                }
            }

            return new TagValue(utcSampleTime, numericValue, textValue, quality, units);
        }


        private static ArchiveCandidateValue GetArchiveCandidateValueFromHashValues(HashEntry[] values) {
            var value = GetTagValueFromHashValues(values);
            var min = Double.NaN;
            var max = Double.NaN;

            foreach (var item in values) {
                switch (item.Name.ToString()) {
                    case "MI":
                        min = (double) item.Value;
                        break;
                    case "MA":
                        max = (double) item.Value;
                        break;
                }
            }

            return new ArchiveCandidateValue(value, min, max);
        }


        /// <summary>
        /// Loads the snapshot value for the specified tag from Redis.
        /// </summary>
        /// <param name="historian">The historian.</param>
        /// <param name="tagId">The tag ID.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the snapshot value.
        /// </returns>
        private static async Task<TagValue> LoadSnapshotValue(RedisHistorian historian, string tagId, CancellationToken cancellationToken) {
            var key = historian.GetKeyForSnapshotData(tagId);
            var valuesTask = historian.Connection.GetDatabase().HashGetAllAsync(key);
            await Task.WhenAny(valuesTask, Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (valuesTask.Result.Length == 0) {
                return null;
            }

            return GetTagValueFromHashValues(valuesTask.Result);
        }


        /// <summary>
        /// Saves the specified value to tag's snapshot hash in the Redis database.
        /// </summary>
        /// <param name="value">The new snapshot value.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will save the snapshot value.
        /// </returns>
        private async Task SaveSnapshotValueInternal(TagValue value, CancellationToken cancellationToken) {
            await Task.WhenAny(_historian.Connection.GetDatabase().HashSetAsync(_snapshotKey, GetHashEntriesForTagValue(value)),
                               Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }


        /// <summary>
        /// Loads the archive candidate value for the specified tag from Redis.
        /// </summary>
        /// <param name="historian">The historian.</param>
        /// <param name="tagId">The tag ID.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the archive candidate value.
        /// </returns>
        private static async Task<ArchiveCandidateValue> LoadArchiveCandidateValue(RedisHistorian historian, string tagId, CancellationToken cancellationToken) {
            var key = historian.GetKeyForArchiveCandidateData(tagId);
            var valuesTask = historian.Connection.GetDatabase().HashGetAllAsync(key);
            await Task.WhenAny(valuesTask, Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (valuesTask.Result.Length == 0) {
                return null;
            }

            return GetArchiveCandidateValueFromHashValues(valuesTask.Result);
        }


        /// <summary>
        /// Saves the specified value to tag's archive candidate hash in the Redis database.
        /// </summary>
        /// <param name="value">The new archive candidate value.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will save the archive candidate value.
        /// </returns>
        private async Task SaveArchiveCandidateValueInternal(ArchiveCandidateValue value, CancellationToken cancellationToken) {
            if (_archiveCandidateValue?.Value == null || (value?.Value != null && value.Value.UtcSampleTime > _archiveCandidateValue.Value.UtcSampleTime)) {
                _archiveCandidateValue = value;
                await Task.WhenAny(_historian.Connection.GetDatabase().HashSetAsync(_archiveCandidateKey, GetHashEntriesForArchiveCandidateValue(value)),
                                   Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }


        /// <summary>
        /// Loads the last-archived value for the specified tag.
        /// </summary>
        /// <param name="historian">The historian.</param>
        /// <param name="tagId">The tag ID.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the last-archived value for the tag.
        /// </returns>
        private static async Task<TagValue> LoadLastArchivedValue(RedisHistorian historian, string tagId, CancellationToken cancellationToken) {
            var key = historian.GetKeyForRawData(tagId);
            var lastArchivedValueTask = historian.Connection.GetDatabase().SortedSetRangeByScoreAsync(key, Double.NegativeInfinity, Double.PositiveInfinity, Exclude.None, Order.Descending, 0, 1, CommandFlags.None);

            await Task.WhenAny(lastArchivedValueTask, Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            var value = lastArchivedValueTask.Result.FirstOrDefault();
            return value == RedisValue.Null
                ? null
                : JsonConvert.DeserializeObject<RedisTagValue>(value)?.ToTagValue();
        }


        /// <summary>
        /// Writes archive values to Redis.
        /// </summary>
        /// <param name="values">The values to write.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will perform the write.
        /// </returns>
        private async Task SaveArchiveValues(IEnumerable<TagValue> values, CancellationToken cancellationToken) {
            if (!(values?.Any() ?? false)) {
                return;
            }

            var entries = values.Select(x => RedisTagValue.FromTagValue(x))
                                .Select(x => new SortedSetEntry(JsonConvert.SerializeObject(x), GetScoreForSampleTime(x.UtcSampleTime)))
                                .ToArray();

            await Task.WhenAny(_historian.Connection.GetDatabase().SortedSetAddAsync(_archiveKey, entries, CommandFlags.None),
                               Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }


        /// <summary>
        /// Gets the sorted set score for the specified sample time.
        /// </summary>
        /// <param name="utcSampleTime">The UTC sample time to get the score for.</param>
        /// <returns>
        /// The score for the sample time.
        /// </returns>
        private long GetScoreForSampleTime(DateTime utcSampleTime) {
            return utcSampleTime.Ticks;
        }


        /// <summary>
        /// Gets raw data samples from Redis.
        /// </summary>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="sampleCount">The maximum number of samples to return.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the raw tag values.
        /// </returns>
        internal async Task<IEnumerable<TagValue>> GetRawValues(DateTime utcStartTime, DateTime utcEndTime, int sampleCount, CancellationToken cancellationToken) {
            var startTimeScore = GetScoreForSampleTime(utcStartTime);
            var endTimeScore = GetScoreForSampleTime(utcEndTime);
            if (endTimeScore < startTimeScore) {
                var tmp = startTimeScore;
                startTimeScore = endTimeScore;
                endTimeScore = tmp;
            }

            if (sampleCount < 1 || sampleCount > MaximumRawSampleCount) {
                sampleCount = MaximumRawSampleCount;
            }

            // TODO: investigate use of Lua scripts to select raw values immediately outside the boundaries of the data query

            var db = _historian.Connection.GetDatabase();
            var rawSampleValuesTask = db.SortedSetRangeByScoreWithScoresAsync(_archiveKey, 
                                                                              startTimeScore, 
                                                                              endTimeScore, 
                                                                              Exclude.None, 
                                                                              Order.Ascending, 
                                                                              0,
                                                                              sampleCount,
                                                                              CommandFlags.None);
            await Task.WhenAny(rawSampleValuesTask, Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            var rawSampleValues = rawSampleValuesTask.Result;

            var preBoundValues = new List<TagValue>();

            var first = rawSampleValues.FirstOrDefault();
            if (first.Score > startTimeScore) {
                // The first raw sample is later than that start time the user specified.  We'll find 
                // the rank of the first raw value we got back, and get the item at the index 
                // immediately before that one, of possible.

                var rankTask = db.SortedSetRankAsync(_archiveKey, first.Element);
                await Task.WhenAny(rankTask, Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                if (rankTask.Result > 0) {
                    var preBoundValueTask = db.SortedSetRangeByRankAsync(_archiveKey, rankTask.Result.Value - 1, rankTask.Result.Value - 1);
                    await Task.WhenAny(preBoundValueTask, Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();

                    preBoundValues.AddRange(preBoundValueTask.Result.Select(x => JsonConvert.DeserializeObject<RedisTagValue>(x)?.ToTagValue()));
                }
            }

            var nonArchiveValues = new List<TagValue>();

            var archiveCandidate = _archiveCandidateValue;
            var snapshot = SnapshotValue;

            if (rawSampleValues.Length < sampleCount && (archiveCandidate?.Value?.UtcSampleTime ?? DateTime.MaxValue) <= utcEndTime) {
                nonArchiveValues.Add(archiveCandidate.Value);
            }
            if ((rawSampleValues.Length + nonArchiveValues.Count) < sampleCount && (snapshot?.UtcSampleTime ?? DateTime.MaxValue) <= utcEndTime) {
                nonArchiveValues.Add(snapshot);
            }

            return preBoundValues.Concat(rawSampleValues.Select(x => JsonConvert.DeserializeObject<RedisTagValue>(x.Element)?.ToTagValue())).Concat(nonArchiveValues).ToArray();
        }

        #endregion

    }
}
