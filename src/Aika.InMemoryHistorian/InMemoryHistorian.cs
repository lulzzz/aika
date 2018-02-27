using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika;
using Aika.StateSets;
using Aika.Tags;
using Microsoft.Extensions.Logging;

namespace Aika.Implementations.InMemory {
    /// <summary>
    /// <see cref="IHistorian"/> implementation that uses an in-memory data store.
    /// </summary>
    public sealed class InMemoryHistorian : HistorianBase {

        /// <summary>
        /// Holds tag definitions, indexed by ID.
        /// </summary>
        private readonly ConcurrentDictionary<string, InMemoryTagDefinition> _tags = new ConcurrentDictionary<string, InMemoryTagDefinition>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Holds state set definitions.
        /// </summary>
        private readonly ConcurrentDictionary<string, StateSet> _stateSets = new ConcurrentDictionary<string, StateSet>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Holds archived tag values.
        /// </summary>
        private readonly ConcurrentDictionary<string, RawDataSet> _archive = new ConcurrentDictionary<string, RawDataSet>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Holds archive candidate values.
        /// </summary>
        private readonly ConcurrentDictionary<string, ArchiveCandidateValue> _archiveCandidates = new ConcurrentDictionary<string, ArchiveCandidateValue>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The mamximum number of raw samples that can be requested in a single query.
        /// </summary>
        public const int MaxRawSampleCount = 5000;


        /// <summary>
        /// Gets the historian description.
        /// </summary>
        public override string Description {
            get { return "In-memory data store."; }
        }


        /// <summary>
        /// Creates a new <see cref="InMemoryHistorian"/> object.
        /// </summary>
        /// <param name="taskRunner">The task runner to use.</param>
        /// <param name="loggerFactory">The logger factory to use.</param>
        public InMemoryHistorian(ITaskRunner taskRunner, ILoggerFactory loggerFactory) : base(taskRunner, loggerFactory) { }


        /// <summary>
        /// Initialzizes the historian.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will initialize the historian.
        /// </returns>
        protected override Task Init(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }


        /// <summary>
        /// Performs a tag search.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="filter">The tag search filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A page of search results.
        /// </returns>
        protected override Task<IEnumerable<TagDefinition>> FindTags(ClaimsPrincipal identity, TagDefinitionFilter filter, CancellationToken cancellationToken) {
            IEnumerable<TagDefinition> allTags = _tags.Values;
            var result = filter.FilterType == TagDefinitionFilterJoinType.And
                ? (IEnumerable<TagDefinition>) _tags.Values
                : new TagDefinition[0];

            foreach (var clause in filter.FilterClauses) {
                switch (clause.Field) {
                    case TagDefinitionFilterField.Name:
                        if (filter.FilterType == TagDefinitionFilterJoinType.And) {
                            result = result.Where(x => x.Name.Like(clause.Value));
                        }
                        else {
                            result = result.Concat(allTags.Where(x => x.Name.Like(clause.Value)));
                        }
                        break;
                    case TagDefinitionFilterField.Description:
                        if (filter.FilterType == TagDefinitionFilterJoinType.And) {
                            result = result.Where(x => x.Description?.Like(clause.Value) ?? false);
                        }
                        else {
                            result = result.Concat(allTags.Where(x => x.Description?.Like(clause.Value) ?? false));
                        }
                        break;
                    case TagDefinitionFilterField.Units:
                        if (filter.FilterType == TagDefinitionFilterJoinType.And) {
                            result = result.Where(x => x.Units?.Like(clause.Value) ?? false);
                        }
                        else {
                            result = result.Concat(allTags.Where(x => x.Units?.Like(clause.Value) ?? false));
                        }
                        break;
                }
            }

            if (filter.FilterType == TagDefinitionFilterJoinType.Or) {
                result = result.Distinct();
            }

            return Task.FromResult<IEnumerable<TagDefinition>>(result.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToArray());
        }


        /// <summary>
        /// Gets tag definitions by ID or name.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNamesOrIds">The IDs or names of the tags to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary that maps from the tag name or ID specified in <paramref name="tagNamesOrIds"/> 
        /// to the matching tag definition.
        /// </returns>
        protected override Task<IDictionary<string, TagDefinition>> GetTags(ClaimsPrincipal identity, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
            var result = new Dictionary<string, TagDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in tagNamesOrIds) {
                if (String.IsNullOrWhiteSpace(item)) {
                    continue;
                }

                if (_tags.TryGetValue(item, out var tag)) {
                    result[item] = tag;
                    continue;
                }

                tag = _tags.Values.FirstOrDefault(x => String.Equals(item, x.Name, StringComparison.OrdinalIgnoreCase));
                if (tag != null) {
                    result[item] = tag;
                }
            }
            return Task.FromResult<IDictionary<string, TagDefinition>>(result);
        }


        /// <summary>
        /// Gets the data query functions supported by the historian.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The supported data query functions.  This is always an empty collection; aggregation is 
        /// delegated to the built-in <see cref="AikaHistorian"/> implementation.
        /// </returns>
        protected override Task<IEnumerable<DataQueryFunction>> GetAvailableDataQueryFunctions(CancellationToken cancellationToken) {
            return Task.FromResult<IEnumerable<DataQueryFunction>>(new DataQueryFunction[0]);
        }

        
        /// <summary>
        /// Reads raw data for the specified tags.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to read from.</param>
        /// <param name="utcStartTime">The UTC start time for the request.</param>
        /// <param name="utcEndTime">The UTC end time for the request.</param>
        /// <param name="pointCount">
        ///   The number of raw samples to request per tag.  If not specified or less than zero, 
        ///   <see cref="MaxRawSampleCount"/> will be used.
        /// </param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary of query results, indexed by tag.
        /// </returns>
        private Task<IDictionary<TagDefinition, TagValueCollection>> ReadRawDataInternal(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, DateTime utcStartTime, DateTime utcEndTime, int? pointCount, CancellationToken cancellationToken) {
            var result = new Dictionary<TagDefinition, TagValueCollection>();

            foreach (var tag in tags) {
                if (!_archive.TryGetValue(tag.Id, out var rawData)) {
                    continue;
                }

                rawData.Lock.EnterReadLock();
                try {
                    var take = pointCount.HasValue
                        ? pointCount.Value < 1 || pointCount.Value > MaxRawSampleCount
                            ? MaxRawSampleCount
                            : pointCount.Value
                        : MaxRawSampleCount;

                    var nonArchiveValues = new List<TagValue>();
                    if (_archiveCandidates.TryGetValue(tag.Id, out var candidate) && candidate?.Value != null) {
                        nonArchiveValues.Add(candidate.Value);
                    }
                    var snapshot = tag.ReadSnapshotValue(identity);
                    if (snapshot != null) {
                        nonArchiveValues.Add(snapshot);
                    }

                    var selected = rawData.Values.Concat(nonArchiveValues).Where(x => x.UtcSampleTime >= utcStartTime && x.UtcSampleTime <= utcEndTime).Take(take);

                    result[tag] = new TagValueCollection() {
                        Values = selected.ToArray(),
                        VisualizationHint = TagValueCollectionVisualizationHint.TrailingEdge
                    };
                }
                finally {
                    rawData.Lock.ExitReadLock();
                }
            }

            return Task.FromResult<IDictionary<TagDefinition, TagValueCollection>>(result);
        }


        /// <summary>
        /// Reads raw data for the specified tags.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to read from.</param>
        /// <param name="utcStartTime">The UTC start time for the request.</param>
        /// <param name="utcEndTime">The UTC end time for the request.</param>
        /// <param name="pointCount">
        ///   The number of raw samples to request per tag.  If less than zero or greater than 
        ///   <see cref="MaxRawSampleCount"/>, <see cref="MaxRawSampleCount"/> will be used instead.
        /// </param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary of query results, indexed by tag.
        /// </returns>
        protected override Task<IDictionary<TagDefinition, TagValueCollection>> ReadRawData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken) {
            return ReadRawDataInternal(identity, tags, utcStartTime, utcEndTime, pointCount, cancellationToken);
        }


        /// <summary>
        /// This method is not implemented; processed data queries are handled directly by the <see cref="AikaHistorian"/>.
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="tags"></param>
        /// <param name="dataFunction"></param>
        /// <param name="utcStartTime"></param>
        /// <param name="utcEndTime"></param>
        /// <param name="sampleInterval"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException">This method is not implemented; processed data queries are handled directly by the <see cref="AikaHistorian"/>.</exception>
        protected override Task<IDictionary<TagDefinition, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }


        /// <summary>
        /// This method is not implemented; processed data queries are handled directly by the <see cref="AikaHistorian"/>.
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="tags"></param>
        /// <param name="dataFunction"></param>
        /// <param name="utcStartTime"></param>
        /// <param name="utcEndTime"></param>
        /// <param name="pointCount"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException">This method is not implemented; processed data queries are handled directly by the <see cref="AikaHistorian"/>.</exception>
        protected override Task<IDictionary<TagDefinition, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }


        /// <summary>
        /// Gets the last-archived value for the specified tag ID.
        /// </summary>
        /// <param name="tagId">The tag ID.</param>
        /// <returns>
        /// The last-archived <see cref="TagValue"/>.
        /// </returns>
        internal TagValue GetLastArchivedValue(string tagId) {
            if (!_archive.TryGetValue(tagId, out var rawData)) {
                return null;
            }

            rawData.Lock.EnterReadLock();
            try {
                return rawData.LastOrDefault().Value;
            }
            finally {
                rawData.Lock.ExitReadLock();
            }
        }


        /// <summary>
        /// Saves an updated <see cref="ArchiveCandidateValue"/> for a tag.
        /// </summary>
        /// <param name="tagId">The tag ID.</param>
        /// <param name="value">The archive candidate value.</param>
        internal void SaveArchiveCandidateValue(string tagId, ArchiveCandidateValue value) {
            _archiveCandidates[tagId] = value;
        }


        /// <summary>
        /// Writes values into the archive.
        /// </summary>
        /// <param name="tagId">The tag ID.</param>
        /// <param name="values">The values to write.</param>
        /// <returns>
        /// The results of the write.
        /// </returns>
        internal WriteTagValuesResult InsertArchiveValues(string tagId, IEnumerable<TagValue> values) {
            var rawData = _archive.GetOrAdd(tagId, x => new RawDataSet());
            var tag = _tags[tagId];

            rawData.Lock.EnterWriteLock();
            try {
                var latestValue = rawData.LastOrDefault().Value;

                var firstValueToInsert = values.First();
                TagValue lastValueToInsert = null;
                var notes = new List<string>();

                if (latestValue != null && firstValueToInsert.UtcSampleTime <= latestValue.UtcSampleTime) {
                    lastValueToInsert = values.Last();

                    // Remove any existing samples where the sample time is inside the time range in the values being inserted.

                    var keysToRemove = rawData.Keys.Where(x => x >= firstValueToInsert.UtcSampleTime && x <= lastValueToInsert.UtcSampleTime).ToArray();
                    foreach (var key in keysToRemove) {
                        rawData.Remove(key);
                    }

                    notes.Add($"Removed {keysToRemove.Length} existing samples inside the insertion time range.");
                }

                int insertedSampleCount = 0;

                foreach (var item in values) {
                    ++insertedSampleCount;
                    rawData.Add(item.UtcSampleTime, item);
                    if (lastValueToInsert == null || item.UtcSampleTime > lastValueToInsert.UtcSampleTime) {
                        lastValueToInsert = item;
                    }
                }

                return new WriteTagValuesResult(true, insertedSampleCount, firstValueToInsert.UtcSampleTime, lastValueToInsert.UtcSampleTime, notes);
            }
            finally {
                tag.Properties["Sample Count"] = rawData.Count;
                if (rawData.Count > 0) {
                    tag.Properties["Earliest Sample Time"] = rawData.Keys.First().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                    tag.Properties["Latest Sample Time"] = rawData.Keys.Last().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                }
                rawData.Lock.ExitWriteLock();
            }
        }


        protected override Task<TagDefinition> CreateTag(ClaimsPrincipal claimsIdentity, TagSettings tag, CancellationToken cancellationToken) {
            var result = new InMemoryTagDefinition(this, null, tag, new TagMetadata(DateTime.UtcNow, claimsIdentity?.Identity?.Name));
            _tags[result.Id] = result;

            return Task.FromResult<TagDefinition>(result);
        }


        protected override Task<TagDefinition> UpdateTag(ClaimsPrincipal identity, TagDefinition tag, TagSettings update, string description, CancellationToken cancellationToken) {
            if (!_tags.ContainsKey(tag.Id)) {
                throw new ArgumentException("Tag is not registered.", nameof(tag));
            }

            return base.UpdateTag(identity, tag, update, description, cancellationToken);
        }


        protected override Task<bool> DeleteTag(ClaimsPrincipal identity, string tagId, CancellationToken cancellationToken) {
            var result = false;

            if (_tags.TryRemove(tagId, out var tag)) {
                tag.OnDeleted();
                result = true;

                _archive.TryRemove(tag.Id, out var rawData);
                _archiveCandidates.TryRemove(tag.Id, out var candidate);
            }

            return Task.FromResult(result);
        }

        protected override Task<IEnumerable<StateSet>> GetStateSets(ClaimsPrincipal identity, StateSetFilter filter, CancellationToken cancellationToken) {
            IEnumerable<StateSet> stateSets;

            if (String.IsNullOrWhiteSpace(filter.Filter)) {
                stateSets = _stateSets.Values;
            }
            else {
                var f = filter.Filter.ToUpperInvariant();
                stateSets = _stateSets.Values.Where(x => x.Name.ToUpperInvariant().Contains(f));
            }

            var result = stateSets.OrderBy(x => x.Name).Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToArray();
            return Task.FromResult<IEnumerable<StateSet>>(result);
        }

        protected override Task<StateSet> GetStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken) {
            if (_stateSets.TryGetValue(name, out var stateSet)) {
                return Task.FromResult(stateSet);
            }

            return Task.FromResult<StateSet>(null);
        }

        protected override Task<StateSet> CreateStateSet(ClaimsPrincipal identity, StateSetSettings settings, CancellationToken cancellationToken) {
            var result = new StateSet(settings.Name, settings.Description, settings.States);
            if (!_stateSets.TryAdd(result.Name, result)) {
                throw new ArgumentException("A state set with the same name already exists.", nameof(settings));
            }

            return Task.FromResult(result);
        }

        protected override Task<StateSet> UpdateStateSet(ClaimsPrincipal identity, string name, StateSetSettings settings, CancellationToken cancellationToken) {
            var result = new StateSet(name, settings.Description, settings.States);
            _stateSets[result.Name] = result;

            return Task.FromResult(result);
        }

        protected override Task<bool> DeleteStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken) {
            var result = _stateSets.TryRemove(name, out var _);
            return Task.FromResult(result);
        }


        #region [ Disposable ]

        /// <summary>
        /// Releases managed resources.
        /// </summary>
        /// <param name="disposing">Flags if the historian is being disposed or finalized.</param>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _tags.Clear();
                _archive.Clear();
                _archiveCandidates.Clear();
                _stateSets.Clear();
            }
        }

        #endregion

    }
}
