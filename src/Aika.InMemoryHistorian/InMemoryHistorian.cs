using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika;
using Microsoft.Extensions.Logging;

namespace Aika.Historians {
    /// <summary>
    /// <see cref="IHistorian"/> implementation that uses an in-memory data store.
    /// </summary>
    public sealed class InMemoryHistorian : HistorianBase, ITagDataReader, ITagDataWriter, ITagManager {

        private static readonly ConcurrentDictionary<string, InMemoryTagDefinition> _tags = new ConcurrentDictionary<string, InMemoryTagDefinition>(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<string, StateSet> _stateSets = new ConcurrentDictionary<string, StateSet>(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<string, RawDataSet> _rawData = new ConcurrentDictionary<string, RawDataSet>(StringComparer.OrdinalIgnoreCase);


        private const int MaxRawSampleCount = 5000;

        public override bool IsInitialized {
            get { return true; }
        }


        public override string Description {
            get { return "In-memory data store."; }
        }


        public override IDictionary<string, object> Properties {
            get { return new Dictionary<string, object>(); }
        }


        public InMemoryHistorian(ITaskRunner taskRunner, ILoggerFactory loggerFactory) : base(taskRunner, loggerFactory) { }


        public override Task Init(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }


        public override Task<IDictionary<string, bool>> CanReadTagData(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            var result = tagNames.ToDictionary(x => x, x => true);
            return Task.FromResult<IDictionary<string, bool>>(result);
        }


        public override Task<IDictionary<string, bool>> CanWriteTagData(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            var result = tagNames.ToDictionary(x => x, x => true);
            return Task.FromResult<IDictionary<string, bool>>(result);
        }


        public override Task<IEnumerable<TagDefinition>> GetTags(ClaimsPrincipal identity, TagDefinitionFilter filter, CancellationToken cancellationToken) {
            IEnumerable<TagDefinition> allTags = _tags.Values;
            var result = filter.FilterType == TagDefinitionFilterJoinType.And
                ? (IEnumerable<TagDefinition>) _tags.Values
                : new TagDefinition[0];

            foreach (var clause in filter.FilterClauses) {
                switch (clause.Field) {
                    case TagDefinitionFilterField.Name:
                        if (filter.FilterType == TagDefinitionFilterJoinType.And) {
                            result = result.Where(x => x.Name.Contains(clause.Value));
                        }
                        else {
                            result = result.Concat(allTags.Where(x => x.Name.Contains(clause.Value)));
                        }
                        break;
                    case TagDefinitionFilterField.Description:
                        if (filter.FilterType == TagDefinitionFilterJoinType.And) {
                            result = result.Where(x => x.Description?.Contains(clause.Value) ?? false);
                        }
                        else {
                            result = result.Concat(allTags.Where(x => x.Description?.Contains(clause.Value) ?? false));
                        }
                        break;
                    case TagDefinitionFilterField.Units:
                        if (filter.FilterType == TagDefinitionFilterJoinType.And) {
                            result = result.Where(x => x.Units?.Contains(clause.Value) ?? false);
                        }
                        else {
                            result = result.Concat(allTags.Where(x => x.Units?.Contains(clause.Value) ?? false));
                        }
                        break;
                }
            }

            if (filter.FilterType == TagDefinitionFilterJoinType.Or) {
                result = result.Distinct();
            }

            return Task.FromResult<IEnumerable<TagDefinition>>(result.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToArray());
        }


        public override Task<IEnumerable<TagDefinition>> GetTags(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            var result = _tags.Values.Where(x => tagNames.Any(n => String.Equals(n, x.Id, StringComparison.OrdinalIgnoreCase) || String.Equals(n, x.Name, StringComparison.OrdinalIgnoreCase)));
            return Task.FromResult<IEnumerable<TagDefinition>>(result.ToArray());
        }


        public override Task<IEnumerable<DataQueryFunction>> GetAvailableDataQueryFunctions(CancellationToken cancellationToken) {
            return Task.FromResult<IEnumerable<DataQueryFunction>>(new DataQueryFunction[0]);
        }


        public override Task<IDictionary<string, TagValue>> ReadSnapshotData(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            var tags = _tags.Values.Where(x => tagNames.Any(n => String.Equals(n, x.Id, StringComparison.OrdinalIgnoreCase) || String.Equals(n, x.Name, StringComparison.OrdinalIgnoreCase)));
            var result = new Dictionary<string, TagValue>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in tags) {
                result[item.Name] = item.SnapshotValue;
            }
            return Task.FromResult<IDictionary<string, TagValue>>(result);
        }


        private Task<IDictionary<string, TagValueCollection>> ReadRawData(ClaimsPrincipal identity, IEnumerable<string> tagNames, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, int? pointCount, CancellationToken cancellationToken) {
            var result = new Dictionary<string, TagValueCollection>(StringComparer.OrdinalIgnoreCase);

            var tags = _tags.Values.Where(x => tagNames.Any(n => String.Equals(n, x.Id, StringComparison.OrdinalIgnoreCase) || String.Equals(n, x.Name, StringComparison.OrdinalIgnoreCase)));
            foreach (var tag in tags) {
                if (!_rawData.TryGetValue(tag.Id, out var rawData)) {
                    continue;
                }

                rawData.Lock.EnterReadLock();
                try {
                    var take = pointCount.HasValue
                        ? pointCount.Value < 1 || pointCount.Value > MaxRawSampleCount
                            ? MaxRawSampleCount
                            : pointCount.Value
                        : MaxRawSampleCount;
                    var selected = rawData.Values.Concat(tag.SnapshotValue == null ? new TagValue[0] : new[] { tag.SnapshotValue }).Where(x => x.UtcSampleTime >= utcStartTime && x.UtcSampleTime <= utcEndTime).Take(take);

                    result[tag.Name] = new TagValueCollection() {
                        Values = selected.ToArray(),
                        VisualizationHint = TagValueCollectionVisualizationHint.TrailingEdge
                    };
                }
                finally {
                    rawData.Lock.ExitReadLock();
                }
            }

            return Task.FromResult<IDictionary<string, TagValueCollection>>(result);
        }


        public override Task<IDictionary<string, TagValueCollection>> ReadRawData(ClaimsPrincipal identity, IEnumerable<string> tagNames, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken) {
            return ReadRawData(identity, tagNames, utcStartTime, utcEndTime, TimeSpan.Zero, pointCount, cancellationToken);
        }


        public override Task<IDictionary<string, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<string> tagNames, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }


        public override Task<IDictionary<string, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<string> tagNames, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }


        internal TagValue GetLastArchivedValue(string tagId) {
            if (!_rawData.TryGetValue(tagId, out var rawData)) {
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


        internal WriteTagValuesResult InsertArchiveValues(string tagId, IEnumerable<TagValue> values) {
            var rawData = _rawData.GetOrAdd(tagId, x => new RawDataSet());
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


        public override Task<int?> GetTagCount(ClaimsPrincipal claimsIdentity, CancellationToken cancellationToken) {
            return Task.FromResult<int?>(_tags.Count);
        }


        public override Task<TagDefinition> CreateTag(ClaimsPrincipal claimsIdentity, TagSettings tag, CancellationToken cancellationToken) {
            var result = new InMemoryTagDefinition(this, null, tag);
            _tags[result.Id] = result;

            return Task.FromResult<TagDefinition>(result);
        }


        public override Task<TagDefinition> UpdateTag(ClaimsPrincipal identity, string tagId, TagSettings update, CancellationToken cancellationToken) {
            if (!_tags.TryGetValue(tagId, out var tag)) {
                throw new ArgumentException($"Could not find tag with ID or name \"{tagId}\".", nameof(tagId));
            }

            tag.Update(update);
            return Task.FromResult<TagDefinition>(tag);
        }


        public override Task<bool> DeleteTag(ClaimsPrincipal identity, string tagId, CancellationToken cancellationToken) {
            if (!_tags.TryGetValue(tagId, out var tag)) {
                return Task.FromResult(false);
            }

            if (_tags.TryRemove(tag.Id, out var _)) {
                tag.OnDeleted();
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public override Task<IEnumerable<StateSet>> GetStateSets(ClaimsPrincipal identity, StateSetFilter filter, CancellationToken cancellationToken) {
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

        public override Task<StateSet> GetStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken) {
            if (_stateSets.TryGetValue(name, out var stateSet)) {
                return Task.FromResult(stateSet);
            }

            return Task.FromResult<StateSet>(null);
        }

        public override Task<StateSet> CreateStateSet(ClaimsPrincipal identity, string name, IEnumerable<StateSetItem> states, CancellationToken cancellationToken) {
            var result = new StateSet(name, states);
            if (!_stateSets.TryAdd(result.Name, result)) {
                throw new ArgumentException("A state set with the same name already exists.", nameof(name));
            }

            return Task.FromResult(result);
        }

        public override Task<StateSet> UpdateStateSet(ClaimsPrincipal identity, string name, IEnumerable<StateSetItem> states, CancellationToken cancellationToken) {
            var result = new StateSet(name, states);
            _stateSets[result.Name] = result;

            return Task.FromResult(result);
        }

        public override Task<bool> DeleteStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken) {
            var result = _stateSets.TryRemove(name, out var _);
            return Task.FromResult(result);
        }
    }
}
