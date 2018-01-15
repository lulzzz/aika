using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Aika.Elasticsearch.Documents;
using Microsoft.Extensions.Logging;
using Nest;

namespace Aika.Elasticsearch {
    public class ElasticHistorian : HistorianBase {

        public const string TagsIndexName = "tags";

        public const string TagChangeHistoryIndexName = "tag-config-history";

        public const string StateSetsIndexName = "state-sets";

        public const string SnapshotValuesIndexName = "snapshot";

        public const string ArchiveCandidatesIndexName = "archive-temporary";

        public const string ArchiveIndexNamePrefix = "archive-permanent-";

        public override string Description {
            get { return Resources.ElasticHistorian_Description; }
        }

        private bool _isInitialized;

        public override bool IsInitialized {
            get { return _isInitialized; }
        }

        private readonly ConcurrentDictionary<string, object> _properties = new ConcurrentDictionary<string, object>();

        public override IDictionary<string, object> Properties {
            get { return _properties; }
        }

        internal new ITaskRunner TaskRunner {
            get { return base.TaskRunner; }
        }

        internal ElasticClient Client { get; }

        private readonly ConcurrentDictionary<string, ElasticTagDefinition> _tags = new ConcurrentDictionary<string, ElasticTagDefinition>();

        private readonly ConcurrentDictionary<string, StateSet> _stateSets = new ConcurrentDictionary<string, StateSet>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Holds the archive data index names that are known to exist
        /// </summary>
        private readonly ConcurrentDictionary<string, object> _archiveIndices = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private BackgroundSnapshotValuesWriter _snapshotWriter;

        private BackgroundArchiveValuesWriter _archiveWriter;


        public ElasticHistorian(ITaskRunner taskRunner, ElasticClient client, ILoggerFactory loggerFactory) : base(taskRunner, loggerFactory) {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }


        public override async Task Init(CancellationToken cancellationToken) {
            if (_isInitialized) {
                return;
            }

            _snapshotWriter?.Dispose();
            _archiveWriter?.Dispose();

            await CreateFixedIndices(cancellationToken).ConfigureAwait(false);
            await LoadArchiveIndexNames(cancellationToken).ConfigureAwait(false);
            await LoadTags(cancellationToken).ConfigureAwait(false);

            _snapshotWriter = new BackgroundSnapshotValuesWriter(this, TimeSpan.FromSeconds(2), LoggerFactory);
            _archiveWriter = new BackgroundArchiveValuesWriter(this, TimeSpan.FromSeconds(2), LoggerFactory);

            TaskRunner.RunBackgroundTask(ct => _snapshotWriter.Execute(ct));
            TaskRunner.RunBackgroundTask(ct => _archiveWriter.Execute(ct));

            _isInitialized = true;
        }


        private async Task CreateFixedIndices(CancellationToken cancellationToken) {
            // Make sure that the fixed indices (tag definitions, state sets, snapshot values, and archive candidate values) exist.
            var existsResponse = await Client.IndexExistsAsync(new IndexExistsRequest(TagsIndexName), cancellationToken).ConfigureAwait(false);
            if (!existsResponse.Exists) {
                await Client.CreateIndexAsync(DocumentMappings.GetTagsIndexDescriptor(TagsIndexName), cancellationToken).ConfigureAwait(false);
            }

            existsResponse = await Client.IndexExistsAsync(new IndexExistsRequest(TagChangeHistoryIndexName), cancellationToken).ConfigureAwait(false);
            if (!existsResponse.Exists) {
                await Client.CreateIndexAsync(DocumentMappings.GetTagsIndexDescriptor(TagChangeHistoryIndexName), cancellationToken).ConfigureAwait(false);
            }

            existsResponse = await Client.IndexExistsAsync(new IndexExistsRequest(StateSetsIndexName), cancellationToken).ConfigureAwait(false);
            if (!existsResponse.Exists) {
                await Client.CreateIndexAsync(DocumentMappings.GetStateSetsIndexDescriptor(StateSetsIndexName), cancellationToken).ConfigureAwait(false);
            }

            existsResponse = await Client.IndexExistsAsync(new IndexExistsRequest(SnapshotValuesIndexName), cancellationToken).ConfigureAwait(false);
            if (!existsResponse.Exists) {
                await Client.CreateIndexAsync(DocumentMappings.GetSnapshotOrArchiveCandidateValuesIndexDescriptor(SnapshotValuesIndexName), cancellationToken).ConfigureAwait(false);
            }

            existsResponse = await Client.IndexExistsAsync(new IndexExistsRequest(ArchiveCandidatesIndexName), cancellationToken).ConfigureAwait(false);
            if (!existsResponse.Exists) {
                await Client.CreateIndexAsync(DocumentMappings.GetSnapshotOrArchiveCandidateValuesIndexDescriptor(ArchiveCandidatesIndexName), cancellationToken).ConfigureAwait(false);
            }
        }


        private async Task LoadArchiveIndexNames(CancellationToken cancellationToken) {
            var response = await Client.GetIndexAsync(ArchiveIndexNamePrefix + "*", null, cancellationToken).ConfigureAwait(false);
            foreach (var item in response.Indices) {
                _archiveIndices[item.Key] = new object();
            }
        }


        private async Task CreateArchiveIndex(DateTime utcSampleTime, CancellationToken cancellationToken) {
            var indexName = DocumentMappings.GetIndexNameForArchiveTagValue(ArchiveIndexNamePrefix, utcSampleTime);
            if (_archiveIndices.ContainsKey(indexName)) {
                return;
            }

            var response = await Client.CreateIndexAsync(DocumentMappings.GetArchiveValuesIndexDescriptor(indexName), cancellationToken).ConfigureAwait(false);
            if (response.IsValid) {
                _archiveIndices[indexName] = new object();
            }
        }


        public override Task<IDictionary<string, bool>> CanReadTagData(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            var result = tagNames.ToDictionary(x => x, x => true);
            return Task.FromResult<IDictionary<string, bool>>(result);
        }

        public override Task<IDictionary<string, bool>> CanWriteTagData(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            var result = tagNames.ToDictionary(x => x, x => true);
            return Task.FromResult<IDictionary<string, bool>>(result);
        }


        public override Task<IEnumerable<StateSet>> GetStateSets(ClaimsPrincipal identity, StateSetFilter filter, CancellationToken cancellationToken) {
            IEnumerable<StateSet> result = _stateSets.Values;

            if (!String.IsNullOrWhiteSpace(filter?.Filter)) {
                result = result.Where(x => x.Name.Contains(filter.Filter));
            }

            var pageSize = filter?.PageSize ?? StateSetFilter.DefaultPageSize;
            var page = filter?.Page ?? 1;

            result = result.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize);

            return Task.FromResult<IEnumerable<StateSet>>(result);
        }


        public override Task<StateSet> GetStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken) {
            if (String.IsNullOrWhiteSpace(name) || !_stateSets.TryGetValue(name, out var result)) {
                return Task.FromResult<StateSet>(null);
            }

            return Task.FromResult(result);
        }


        public override async Task<StateSet> CreateStateSet(ClaimsPrincipal identity, StateSetSettings settings, CancellationToken cancellationToken) {
            if (_stateSets.ContainsKey(settings.Name)) {
                throw new ArgumentException(Resources.Error_StateSetAlreadyExists, nameof(settings));
            }

            var stateSet = new StateSet(settings.Name, settings.Description, settings.States);
            await Client.IndexAsync(stateSet.ToStateSetDocument(), x => x.Index(StateSetsIndexName), cancellationToken).ConfigureAwait(false);

            _stateSets[stateSet.Name] = stateSet;
            return stateSet;
        }


        public override async Task<StateSet> UpdateStateSet(ClaimsPrincipal identity, string name, StateSetSettings settings, CancellationToken cancellationToken) {
            if (!_stateSets.TryGetValue(name, out var existing)) {
                throw new ArgumentException(Resources.Error_StateSetDoesNotExist, nameof(settings));
            }

            var newValue = new StateSet(existing.Name, settings.Description, settings.States);
            await Client.IndexAsync(newValue.ToStateSetDocument(), x => x.Index(StateSetsIndexName), cancellationToken).ConfigureAwait(false);

            _stateSets[newValue.Name] = newValue;
            return newValue;
        }


        public override async Task<bool> DeleteStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken) {
            if (!_stateSets.TryGetValue(name, out var value)) {
                return false;
            }

            await Client.DeleteAsync(new DocumentPath<StateSetDocument>(value.Name), x => x.Index(StateSetsIndexName), cancellationToken).ConfigureAwait(false);
            _stateSets.TryRemove(value.Name, out var _);
            return true;
        }


        public override async Task<TagDefinition> CreateTag(ClaimsPrincipal identity, TagSettings tag, CancellationToken cancellationToken) {
            var exists = _tags.Values.Any(x => x.Name.Equals(tag.Name, StringComparison.OrdinalIgnoreCase));
            if (exists) {
                throw new ArgumentException(Resources.Error_TagAlreadyExists, nameof(tag));
            }

            TagSecurity security = null;
            if (identity != null) {
                var idClaim = identity.FindFirst(ClaimTypes.NameIdentifier);
                if (idClaim != null) {
                    security = new TagSecurity() {
                        Policy = "Owner",
                        Allow = new [] {
                            new TagSecurityEntry() {
                                ClaimType = ClaimTypes.NameIdentifier, Value = idClaim.Value
                            }
                        }
                    };
                }
            }

            var tagDefinition = new ElasticTagDefinition(this, Guid.NewGuid(), tag, security == null ? null : new[] { security }, null, null);

            await Client.IndexAsync(tagDefinition.ToTagDocument(), x => x.Index(TagsIndexName), cancellationToken).ConfigureAwait(false);

            _tags[tagDefinition.Id] = tagDefinition;
            return tagDefinition;
        }


        public override async Task<TagDefinition> UpdateTag(ClaimsPrincipal identity, string tagId, TagSettings update, string description, CancellationToken cancellationToken) {
            if (!_tags.TryGetValue(tagId, out var tag)) {
                throw new ArgumentException(Resources.Error_TagDoesNotExist, nameof(tagId));
            }

            var oldConfig = tag.ToTagDocument();

            var change = tag.Update(update, identity, description);
            await Task.WhenAll(Client.IndexAsync(tag.ToTagDocument(), x => x.Index(TagsIndexName), cancellationToken),
                               Client.IndexAsync(new TagChangeHistoryDocument() {
                                   Id = change.Id,
                                   TagId = tag.IdAsGuid,
                                   UtcTime = change.UtcTime,
                                   User = change.User,
                                   Description = change.Description,
                                   PreviousVersion = oldConfig
                               })).ConfigureAwait(false);

            return tag;
        }


        public override async Task<bool> DeleteTag(ClaimsPrincipal identity, string tagId, CancellationToken cancellationToken) {
            if (!_tags.TryGetValue(tagId, out var tag)) {
                return false;
            }

            await Task.WhenAll(
                Client.DeleteAsync(new DocumentPath<TagDocument>(tag.IdAsGuid), x => x.Index(TagsIndexName), cancellationToken),
                Client.DeleteByQueryAsync<TagValueDocument>(x => x.AllIndices().Query(q => q.Term(d => d.TagId, tag.IdAsGuid))),
                Client.DeleteByQueryAsync<TagChangeHistoryDocument>(x => x.Index(TagChangeHistoryIndexName).Query(q => q.Term(d => d.TagId, tag.IdAsGuid)))
            ).ConfigureAwait(false);

            _tags.TryRemove(tag.Id, out var _);
            return true;
        }


        public override Task<IEnumerable<DataQueryFunction>> GetAvailableDataQueryFunctions(CancellationToken cancellationToken) {
            return Task.FromResult<IEnumerable<DataQueryFunction>>(new DataQueryFunction[0]);
        }


        public override Task<int?> GetTagCount(ClaimsPrincipal identity, CancellationToken cancellationToken) {
            return Task.FromResult<int?>(_tags.Count);
        }


        private async Task LoadTags(CancellationToken cancellationToken) {
            const int pageSize = 100;
            var page = 0;

            var snapshotValuesTask = LoadSnapshotValues(SnapshotValuesIndexName, cancellationToken);
            var archiveCandidateValuesTask = LoadSnapshotValues(ArchiveCandidatesIndexName, cancellationToken);

            await Task.WhenAll(snapshotValuesTask, archiveCandidateValuesTask).ConfigureAwait(false);

            var tasks = new List<Task>();

            var archiveIndexNames = _archiveIndices.Keys.OrderByDescending(x => x).ToArray();

            var @continue = false;
            do {
                ++page;
                var results = await Client.SearchAsync<TagDocument>(x => x.Index(TagsIndexName).From((page - 1) * pageSize).Size(pageSize), cancellationToken).ConfigureAwait(false);
                @continue = results.Hits.Count == pageSize;

                if (results.Hits.Count > 0) {
                    tasks.Add(Task.Run(async () => {
                        foreach (var hit in results.Hits) {
                            cancellationToken.ThrowIfCancellationRequested();
                            var settings = hit.Source.ToTagSettings();

                            snapshotValuesTask.Result.TryGetValue(hit.Source.Id, out var snapshot);
                            archiveCandidateValuesTask.Result.TryGetValue(hit.Source.Id, out var archiveCandidate);
                            var lastArchived = await LoadLastArchivedValue(hit.Source.Id, archiveIndexNames, cancellationToken).ConfigureAwait(false);

                            var tag = new ElasticTagDefinition(this, 
                                                               hit.Source.Id, 
                                                               settings,
                                                               hit.Source.Security,
                                                               new InitialTagValues(snapshot?.ToTagValue(settings.Units), lastArchived?.ToTagValue(settings.Units), archiveCandidate?.ToTagValue(settings.Units)), 
                                                               null);

                            _tags[tag.Id] = tag;
                        }
                    }));
                }
            } while (@continue);

            await Task.WhenAll(tasks).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }


        private async Task<IDictionary<Guid, TagValueDocument>> LoadSnapshotValues(string indexName, CancellationToken cancellationToken) {
            const int pageSize = 100;
            var page = 0;

            var result = new ConcurrentDictionary<Guid, TagValueDocument>();

            var tasks = new List<Task>();

            var @continue = false;
            do {
                ++page;
                var results = await Client.SearchAsync<TagValueDocument>(x => x.Index(indexName).From((page - 1) * pageSize).Size(pageSize), cancellationToken).ConfigureAwait(false);
                @continue = results.Hits.Count == pageSize;

                if (results.Hits.Count > 0) {
                    tasks.Add(Task.Run(() => {
                        foreach (var hit in results.Hits) {
                            cancellationToken.ThrowIfCancellationRequested();
                            result[hit.Source.TagId] = hit.Source;
                        }
                    }));
                }
            } while (@continue);

            await Task.WhenAll(tasks).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            return result;
        }


        private async Task<TagValueDocument> LoadLastArchivedValue(Guid tagId, string[] indexNames, CancellationToken cancellationToken) {
            foreach (var index in indexNames) {
                var response = await Client.SearchAsync<TagValueDocument>(x => x.Index(index).Query(q => q.Term(t => t.TagId, tagId)).Sort(s => s.Descending(f => f.UtcSampleTime)).From(0).Size(1), cancellationToken).ConfigureAwait(false);
                if (response.Hits.Count > 0) {
                    return response.Hits.First().Source;
                }
            }

            return null;
        }


        public override Task<IEnumerable<TagDefinition>> GetTags(ClaimsPrincipal identity, TagDefinitionFilter filter, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }


        public override Task<IEnumerable<TagDefinition>> GetTags(ClaimsPrincipal identity, IEnumerable<string> tagIdsOrNames, CancellationToken cancellationToken) {
            if (tagIdsOrNames == null) {
                return Task.FromResult<IEnumerable<TagDefinition>>(new TagDefinition[0]);
            }

            var result = _tags.Values
                              .Where(x => tagIdsOrNames.Contains(x.Id) || tagIdsOrNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase))
                              .OrderBy(x => x.Name)
                              .ToArray();
            return Task.FromResult<IEnumerable<TagDefinition>>(result);
        }


        internal ElasticTagDefinition GetTagById(Guid id) {
            return _tags.TryGetValue(id.ToString(), out var tag) ? tag : null;
        }


        public override Task<IDictionary<string, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<string> tagNames, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        public override Task<IDictionary<string, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<string> tagNames, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        public override Task<IDictionary<string, TagValueCollection>> ReadRawData(ClaimsPrincipal identity, IEnumerable<string> tagNames, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        public override Task<IDictionary<string, TagValue>> ReadSnapshotData(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }


        internal Task WriteSnapshotValue(ElasticTagDefinition tag, TagValue value, CancellationToken cancellationToken) {
            _snapshotWriter.WriteValue(tag.IdAsGuid, value);
            return Task.CompletedTask;
        }


        internal async Task<WriteTagValuesResult> InsertArchiveValues(ElasticTagDefinition tag, IEnumerable<TagValue> values, TagValue nextArchiveCandidate, CancellationToken cancellationToken) {
            foreach (var value in (values ?? new TagValue[0]).Concat(new[] { nextArchiveCandidate })) {
                if (value == null) {
                    continue;
                }

                await CreateArchiveIndex(value.UtcSampleTime, cancellationToken).ConfigureAwait(false);
            }

            _archiveWriter.WriteValues(tag, values, nextArchiveCandidate);

            var count = values?.Count() ?? 0;
            return new WriteTagValuesResult(count > 0,
                                            count,
                                            values?.FirstOrDefault()?.UtcSampleTime ?? null,
                                            values?.LastOrDefault()?.UtcSampleTime ?? null,
                                            count == 0 ? null : new[] { Resources.ElasticHistorian_ArchiveWritePendingMessage });
        }

    }
}
