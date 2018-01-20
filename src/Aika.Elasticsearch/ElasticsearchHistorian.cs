using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Aika.Elasticsearch.Documents;
using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Nest;

namespace Aika.Elasticsearch {

    /// <summary>
    /// Aika historian that uses Elasticsearch for storage and aggregation.
    /// </summary>
    public class ElasticsearchHistorian : HistorianBase {


        public const string TagsIndexName = "aika-tags";

        public const string TagChangeHistoryIndexName = "aika-tag-config-history";

        public const string StateSetsIndexName = "aika-state-sets";

        public const string SnapshotValuesIndexName = "aika-snapshot";

        public const string ArchiveCandidatesIndexName = "aika-archive-temporary";

        public const string ArchiveIndexNamePrefix = "aika-archive-permanent-";

        private const string DataQueryIndices = ArchiveIndexNamePrefix + "*," + ArchiveCandidatesIndexName + "," + SnapshotValuesIndexName;

        private const int MaxSamplesPerTagPerQuery = 5000;

        private const int MaxSamplesPerQuery = 20000;

        private const int MaxTagsPerQuery = 100;

        private const string IsoDateFormat = "yyyy-MM-ddTHH:mm:ss.fffffffZ";


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

        private readonly ConcurrentDictionary<string, ElasticsearchTagDefinition> _tagsById = new ConcurrentDictionary<string, ElasticsearchTagDefinition>();

        private readonly ConcurrentDictionary<string, ElasticsearchTagDefinition> _tagsByName = new ConcurrentDictionary<string, ElasticsearchTagDefinition>(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, StateSet> _stateSets = new ConcurrentDictionary<string, StateSet>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Holds the archive data index names that are known to exist
        /// </summary>
        private readonly ConcurrentDictionary<string, object> _archiveIndices = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private BackgroundSnapshotValuesWriter _snapshotWriter;

        private BackgroundArchiveValuesWriter _archiveWriter;

        private static readonly DataQueryFunction[] DataQueryFunctions = {
            new DataQueryFunction(DataQueryFunction.Average, "Average value calculated over a sample interval."),
            new DataQueryFunction(DataQueryFunction.Interpolated, "Interpolated data."),
            new DataQueryFunction(DataQueryFunction.Minimum, "Minimum value over a sample interval."),
            new DataQueryFunction(DataQueryFunction.Maximum, "Maximum value over a sample interval.")
        };


        public ElasticsearchHistorian(ITaskRunner taskRunner, ElasticClient client, ILoggerFactory loggerFactory) : base(taskRunner, loggerFactory) {
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
            await LoadTagsFromElasticsearch(cancellationToken).ConfigureAwait(false);

            _snapshotWriter = new BackgroundSnapshotValuesWriter(this, TimeSpan.FromSeconds(2), LoggerFactory);
            _archiveWriter = new BackgroundArchiveValuesWriter(this, TimeSpan.FromSeconds(2), LoggerFactory);

            TaskRunner.RunBackgroundTask(ct => _snapshotWriter.Execute(ct));
            TaskRunner.RunBackgroundTask(ct => _archiveWriter.Execute(ct));

            _isInitialized = true;
        }

        #region [ Elasticsearch Index Management and Initialization ]

        private async Task CreateFixedIndices(CancellationToken cancellationToken) {
            // Make sure that the fixed indices (tag definitions, change history, state sets, snapshot values, and archive candidate values) exist.
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


        private async Task LoadTagsFromElasticsearch(CancellationToken cancellationToken) {
            const int pageSize = 100;
            var page = 0;

            var snapshotValuesTask = LoadSnapshotValuesFromElasticsearch(SnapshotValuesIndexName, cancellationToken);
            var archiveCandidateValuesTask = LoadSnapshotValuesFromElasticsearch(ArchiveCandidatesIndexName, cancellationToken);

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
                            var metadata = hit.Source.ToTagMetadata();

                            snapshotValuesTask.Result.TryGetValue(hit.Source.Id, out var snapshot);
                            archiveCandidateValuesTask.Result.TryGetValue(hit.Source.Id, out var archiveCandidate);
                            var lastArchived = await LoadLastArchivedValueFromElasticsearch(hit.Source.Id, archiveIndexNames, cancellationToken).ConfigureAwait(false);

                            var tag = new ElasticsearchTagDefinition(this,
                                                               hit.Source.Id,
                                                               settings,
                                                               metadata,
                                                               hit.Source.Security,
                                                               new InitialTagValues(snapshot?.ToTagValue(units: settings.Units), lastArchived?.ToTagValue(units: settings.Units), archiveCandidate?.ToTagValue(units: settings.Units)),
                                                               null);

                            _tagsById[tag.Id] = tag;
                            _tagsByName[tag.Name] = tag;
                        }
                    }));
                }
            } while (@continue);

            await Task.WhenAll(tasks).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }


        private async Task<IDictionary<Guid, TagValueDocument>> LoadSnapshotValuesFromElasticsearch(string indexName, CancellationToken cancellationToken) {
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


        private async Task<TagValueDocument> LoadLastArchivedValueFromElasticsearch(Guid tagId, string[] indexNames, CancellationToken cancellationToken) {
            foreach (var index in indexNames) {
                var response = await Client.SearchAsync<TagValueDocument>(x => x.Index(index).Query(q => q.Term(t => t.TagId, tagId)).Sort(s => s.Descending(f => f.UtcSampleTime)).From(0).Size(1), cancellationToken).ConfigureAwait(false);
                if (response.Hits.Count > 0) {
                    return response.Hits.First().Source;
                }
            }

            return null;
        }

        #endregion

        #region [ State Set Management ]

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

        #endregion

        #region [ Tag Management ]

        public override Task<int?> GetTagCount(ClaimsPrincipal identity, CancellationToken cancellationToken) {
            return Task.FromResult<int?>(_tagsById.Count);
        }


        public override Task<IEnumerable<TagDefinition>> GetTags(ClaimsPrincipal identity, TagDefinitionFilter filter, CancellationToken cancellationToken) {
            var result = filter?.GetMatchingTags(_tagsById.Values, null) ?? new TagDefinition[0];
            return Task.FromResult(result);
        }


        public override Task<IEnumerable<TagDefinition>> GetTags(ClaimsPrincipal identity, IEnumerable<string> tagIdsOrNames, CancellationToken cancellationToken) {
            if (tagIdsOrNames == null) {
                return Task.FromResult<IEnumerable<TagDefinition>>(new TagDefinition[0]);
            }

            var result = GetTagsByIdOrName(identity, tagIdsOrNames);
            return Task.FromResult<IEnumerable<TagDefinition>>(result.OrderBy(x => x.Name).ToArray());
        }


        private IEnumerable<ElasticsearchTagDefinition> GetTagsByIdOrName(ClaimsPrincipal identity, IEnumerable<string> tagIdsOrNames) {
            if (tagIdsOrNames == null) {
                return new ElasticsearchTagDefinition[0];
            }

            var result = new HashSet<ElasticsearchTagDefinition>();
            foreach (var item in tagIdsOrNames) {
                if (_tagsById.TryGetValue(item, out var tag)) {
                    result.Add(tag);
                }
                else if (_tagsByName.TryGetValue(item, out tag)) {
                    result.Add(tag);
                }
            }

            return result.OrderBy(x => x.Name).ToArray();
        }


        internal ElasticsearchTagDefinition GetTagById(Guid id) {
            return _tagsById.TryGetValue(id.ToString(), out var tag) ? tag : null;
        }


        public override async Task<TagDefinition> CreateTag(ClaimsPrincipal identity, TagSettings tag, CancellationToken cancellationToken) {
            var exists = _tagsByName.ContainsKey(tag.Name);
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

            var metadata = new TagMetadata(DateTime.UtcNow, identity?.Identity?.Name);
            var tagDefinition = new ElasticsearchTagDefinition(this, Guid.NewGuid(), tag, metadata, security == null ? null : new[] { security }, null, null);

            await Client.IndexAsync(tagDefinition.ToTagDocument(), x => x.Index(TagsIndexName), cancellationToken).ConfigureAwait(false);

            _tagsById[tagDefinition.Id] = tagDefinition;
            _tagsById[tagDefinition.Name] = tagDefinition;
            return tagDefinition;
        }


        public override async Task<TagDefinition> UpdateTag(ClaimsPrincipal identity, string tagId, TagSettings update, string description, CancellationToken cancellationToken) {
            if (!_tagsById.TryGetValue(tagId, out var tag)) {
                throw new ArgumentException(Resources.Error_TagDoesNotExist, nameof(tagId));
            }

            var nameChange = !String.IsNullOrWhiteSpace(update.Name) && !tag.Name.Equals(update.Name, StringComparison.OrdinalIgnoreCase);
            if (nameChange && _tagsByName.ContainsKey(update.Name)) {
                throw new ArgumentException(Resources.Error_TagAlreadyExists, nameof(update));
            }

            var oldConfig = tag.ToTagDocument();

            var change = tag.Update(update, identity, description);
            if (nameChange) {
                _tagsByName.TryRemove(oldConfig.Name, out var _);
                _tagsByName[tag.Name] = tag;
            }

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
            if (!_tagsById.TryGetValue(tagId, out var tag)) {
                return false;
            }

            await Task.WhenAll(
                Client.DeleteAsync(new DocumentPath<TagDocument>(tag.IdAsGuid), x => x.Index(TagsIndexName), cancellationToken),
                Client.DeleteByQueryAsync<TagValueDocument>(x => x.AllIndices().Query(q => q.Term(d => d.TagId, tag.IdAsGuid))),
                Client.DeleteByQueryAsync<TagChangeHistoryDocument>(x => x.Index(TagChangeHistoryIndexName).Query(q => q.Term(d => d.TagId, tag.IdAsGuid)))
            ).ConfigureAwait(false);

            _tagsById.TryRemove(tag.Id, out var _);
            _tagsByName.TryRemove(tag.Name, out var _);

            return true;
        }

        #endregion

        #region [ Read Tag Data ]

        public override Task<IEnumerable<DataQueryFunction>> GetAvailableDataQueryFunctions(CancellationToken cancellationToken) {
            return Task.FromResult<IEnumerable<DataQueryFunction>>(DataQueryFunctions);
        }


        public override Task<IDictionary<string, bool>> CanReadTagData(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            var result = tagNames.ToDictionary(x => x, x => true);
            return Task.FromResult<IDictionary<string, bool>>(result);
        }


        public override async Task<IDictionary<string, TagValue>> ReadSnapshotData(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            if (tagNames == null) {
                return new Dictionary<string, TagValue>();
            }
            var tags = await GetTags(identity, tagNames, cancellationToken).ConfigureAwait(false);
            return tags.ToDictionary(x => x.Name, x => x.SnapshotValue);
        }


        private static string GetIsoDateString(DateTime time) {
            return time.ToUniversalTime().ToString(IsoDateFormat, CultureInfo.InvariantCulture);
        }


        /// <summary>
        /// Takes a collection of tags that will be queried for data and, based on the estimated 
        /// number of samples that will be returned per tag, the maximum overall number of samples 
        /// that the driver allows us to request, and the number of tags that we allow to be 
        /// specified in a single query to Elasticsearch, breaks the tags up into query groups.
        /// </summary>
        /// <param name="tags">The tags.</param>
        /// <param name="expectedSampleCountPerTag">
        ///     The expected sample count per tag.  The accuracy of this number varies depending on 
        ///     the type of qury that is being performed.  For aggregated data queries, it's more or 
        ///     less accurate, because we are asking for one value per bucket in the query (e.g. 
        ///     hourly average for the last 24 hours).  For plot requests, it would be 5x the number 
        ///     of intervals in the plot request, since plot attempts to return at most 5 samples per 
        ///     interval (for numeric tags).  For raw data queries, it's pretty inaccurate (unless the 
        ///     caller has a good idea of the update frequency of the tags, and even then it's a guess 
        ///     because of exception and compression filtering), because it is just the maximum number 
        ///     of samples that the caller wants to return.
        /// </param>
        /// <returns>
        /// An array of tag groups, where each group is an array of tags.
        /// </returns>
        private ElasticsearchTagDefinition[][] GetTagQueryGroups(IEnumerable<ElasticsearchTagDefinition> tags, int expectedSampleCountPerTag) {
            var result = new List<ElasticsearchTagDefinition[]>();

            List<ElasticsearchTagDefinition> currentGroup = null;

            foreach (var tag in tags) {
                if (currentGroup == null) {
                    currentGroup = new List<ElasticsearchTagDefinition>();
                }
                else if (currentGroup.Count == MaxTagsPerQuery || currentGroup.Count * expectedSampleCountPerTag > MaxSamplesPerQuery) {
                    result.Add(currentGroup.ToArray());
                    currentGroup = new List<ElasticsearchTagDefinition>();
                }

                currentGroup.Add(tag);
            }

            if (currentGroup?.Count > 0) {
                result.Add(currentGroup.ToArray());
            }

            return result.ToArray();
        }


        private SearchDescriptor<TagValueDocument> AddRawDataQuerySearchDescriptor(SearchDescriptor<TagValueDocument> selector, Guid tagId, string utcStartTime, string utcEndTime, int pointCount) {
            return selector.Index(DataQueryIndices)
                           .Query(
                               query => query.Bool(
                                   q1 => q1.Must(
                                       q1m1 => q1m1.Term(x => x.TagId, tagId),
                                       q1m2 => q1m2.TermRange(
                                           q1m2r1 => q1m2r1.Field(x => x.UtcSampleTime)
                                                           .GreaterThanOrEquals(utcStartTime)
                                                           .LessThanOrEquals(utcEndTime)
                                       )
                                   )
                               )
                           )
                           .Sort(sort => sort.Ascending(x => x.UtcSampleTime))
                           .From(0)
                           .Size(pointCount);
        }


        private SearchDescriptor<TagValueDocument> AddPreviousSampleSearchDescriptor(SearchDescriptor<TagValueDocument> selector, Guid tagId, string utcSampleTime) {
            return selector.Index(DataQueryIndices)
                           .Query(
                               query => query.Bool(
                                   q1 => q1.Must(
                                       q1m1 => q1m1.Term(x => x.TagId, tagId),
                                       q1m2 => q1m2.TermRange(
                                           q1m2r1 => q1m2r1.Field(x => x.UtcSampleTime)
                                                           .LessThan(utcSampleTime)
                                       )
                                   )
                               )
                           )
                           .Sort(sort => sort.Descending(x => x.UtcSampleTime))
                           .Size(1);
        }


        private SearchDescriptor<TagValueDocument> AddNextSampleSearchDescriptor(SearchDescriptor<TagValueDocument> selector, Guid tagId, string utcSampleTime) {
            return selector.Index(DataQueryIndices)
                           .Query(
                               query => query.Bool(
                                   q1 => q1.Must(
                                       q1m1 => q1m1.Term(x => x.TagId, tagId),
                                       q1m2 => q1m2.TermRange(
                                           q1m2r1 => q1m2r1.Field(x => x.UtcSampleTime)
                                                           .GreaterThan(utcSampleTime)
                                       )
                                   )
                               )
                           )
                           .Sort(sort => sort.Ascending(x => x.UtcSampleTime))
                           .Size(1);
        }


        private SearchDescriptor<TagValueDocument> AddAggregatedDataQuerySearchDescriptor(SearchDescriptor<TagValueDocument> selector, Guid tagId, string utcStartTime, string utcEndTime) {
            return selector.Index(DataQueryIndices)
                           .Size(0)
                           .Query(query => query.Bool(
                                    q1 => q1.Must(
                                        q1m1 => q1m1.Term(x => x.TagId, tagId),
                                        q1m2 => q1m2.TermRange(
                                            q1m2r1 => q1m2r1.Field(x => x.UtcSampleTime)
                                                            .GreaterThanOrEquals(utcStartTime)
                                                            .LessThanOrEquals(utcEndTime)
                                        )
                                    )
                                )
                            );
        }


        public override async Task<IDictionary<string, TagValueCollection>> ReadRawData(ClaimsPrincipal identity, IEnumerable<string> tagNames, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken) {
            if (tagNames == null) {
                return new Dictionary<string, TagValueCollection>();
            }

            var result = new ConcurrentDictionary<string, TagValueCollection>();
            var tags = GetTagsByIdOrName(identity, tagNames);

            var gte = GetIsoDateString(utcStartTime);
            var lte = GetIsoDateString(utcEndTime);

            var take = pointCount < 1 || pointCount > MaxSamplesPerTagPerQuery
                ? MaxSamplesPerTagPerQuery
                : pointCount;

            var tagGroups = GetTagQueryGroups(tags, take);

            var queries = tagGroups.Select(grp => ReadRawDataInternal(grp, gte, lte, take, cancellationToken).ContinueWith(t => {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var item in t.Result) {
                    result[item.Key] = item.Value;
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion)).ToArray();

            await Task.WhenAny(Task.WhenAll(queries), Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            
            return result;
        }


        private async Task<IDictionary<string, TagValueCollection>> ReadRawDataInternal(IEnumerable<ElasticsearchTagDefinition> tags, string utcStartTime, string utcEndTime, int pointCount, CancellationToken cancellationToken) {
            var result = new ConcurrentDictionary<string, TagValueCollection>();

            var multiSearchRequest = new MultiSearchDescriptor();
            foreach (var tag in tags) {
                multiSearchRequest = multiSearchRequest
                    .Search<TagValueDocument>(selector => AddRawDataQuerySearchDescriptor(selector, tag.IdAsGuid, utcStartTime, utcEndTime, pointCount));
            }

            var response = await Client.MultiSearchAsync(multiSearchRequest, cancellationToken).ConfigureAwait(false);

            var tagEnumerator = tags.GetEnumerator();
            foreach (var item in response.GetResponses<TagValueDocument>()) {
                if (!tagEnumerator.MoveNext()) {
                    break;
                }

                var tag = tagEnumerator.Current;
                if (tag == null) {
                    continue;
                }

                result[tag.Name] = new TagValueCollection() {
                    VisualizationHint = TagValueCollectionVisualizationHint.TrailingEdge,
                    Values = item.Documents.Select(x => x.ToTagValue(units: tag.Units)).ToArray()
                };
            }

            return result;
        }


        public override Task<IDictionary<string, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<string> tagNames, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken) {
            if (pointCount < 1) {
                pointCount = 1;
            }
            var sampleInterval = TimeSpan.FromMilliseconds((utcEndTime - utcStartTime).TotalMilliseconds / pointCount);
            return ReadProcessedData(identity, tagNames, dataFunction, utcStartTime, utcEndTime, sampleInterval, cancellationToken);
        }


        public override async Task<IDictionary<string, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<string> tagNames, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, CancellationToken cancellationToken) {
            if (tagNames == null) {
                return new Dictionary<string, TagValueCollection>();
            }

            var result = new ConcurrentDictionary<string, TagValueCollection>();
            var tags = GetTagsByIdOrName(identity, tagNames);

            if (!DataQueryFunction.Interpolated.Equals(dataFunction, StringComparison.OrdinalIgnoreCase)) {
                // Move the start time back by one interval so that we get a result at the start time that 
                // was actually asked for.
                utcStartTime = utcStartTime.Subtract(sampleInterval);
            }

            // If we would exceed MaxSamplesPerTagPerQuery with the start time, end time, and sample 
            // interval specified, modify the end time so that we won't get more than 
            // MaxSamplesPerTagPerQuery back.
            var pointCount = (int) Math.Ceiling((utcEndTime - utcStartTime).TotalMilliseconds / sampleInterval.TotalMilliseconds);
            if (pointCount > MaxSamplesPerTagPerQuery) {
                utcEndTime = utcStartTime.Add(TimeSpan.FromMilliseconds(sampleInterval.TotalMilliseconds * pointCount));
            }

            var gte = GetIsoDateString(utcStartTime);
            var lte = GetIsoDateString(utcEndTime);

            var tagGroups = GetTagQueryGroups(tags, pointCount);

            IEnumerable<Task<IDictionary<string, TagValueCollection>>> queries = null;

            switch (dataFunction.ToUpperInvariant()) {
                case DataQueryFunction.Average:
                    queries = tagGroups.Select(grp => ReadAverageDataInternal(grp, gte, utcStartTime, lte, utcEndTime, sampleInterval, cancellationToken));
                    break;
                case DataQueryFunction.Minimum:
                    queries = tagGroups.Select(grp => ReadMinimumDataInternal(grp, gte, utcStartTime, lte, utcEndTime, sampleInterval, cancellationToken));
                    break;
                case DataQueryFunction.Maximum:
                    queries = tagGroups.Select(grp => ReadMaximumDataInternal(grp, gte, utcStartTime, lte, utcEndTime, sampleInterval, cancellationToken));
                    break;
                case DataQueryFunction.Interpolated:
                    queries = tagGroups.Select(grp => ReadInterpolatedDataInternal(grp, gte, utcStartTime, lte, utcEndTime, sampleInterval, cancellationToken));
                    break;
            }

            if (queries == null) {
                return result;
            }

            var continuations = queries.Select(x => x.ContinueWith(t => {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var item in t.Result) {
                    result[item.Key] = item.Value;
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion)).ToArray();

            await Task.WhenAny(Task.WhenAll(continuations), Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            return result;
        }


        private async Task<IDictionary<string, TagValueCollection>> ReadAverageDataInternal(IEnumerable<ElasticsearchTagDefinition> tags, string utcStartTimeAsString, DateTime utcStartTimeAsDate, string utcEndTimeAsString, DateTime utcEndTimeAsDate, TimeSpan sampleInterval, CancellationToken cancellationToken) {
            var result = new ConcurrentDictionary<string, TagValueCollection>();

            var multiSearchRequest = new MultiSearchDescriptor();
            foreach (var tag in tags) {
                multiSearchRequest = multiSearchRequest
                    .Search<TagValueDocument>(
                        selector => AddAggregatedDataQuerySearchDescriptor(selector, tag.IdAsGuid, utcStartTimeAsString, utcEndTimeAsString)
                            .Aggregations(
                                aa => aa.DateHistogram(
                                    "aika",
                                    dh => dh.Field(
                                        x => x.UtcSampleTime
                                    )
                                    .Interval(sampleInterval)
                                    .MinimumDocumentCount(0)
                                    .ExtendedBoundsDateMath(utcStartTimeAsDate, utcEndTimeAsDate)
                                    .Aggregations(
                                        aa2 => aa2.Average(
                                            "avg_value", 
                                            avg => avg.Field(
                                                x => x.NumericValue
                                            )
                                        )
                                    )
                                )
                            ));
            }

            var response = await Client.MultiSearchAsync(multiSearchRequest, cancellationToken).ConfigureAwait(false);

            var tagEnumerator = tags.GetEnumerator();
            foreach (var item in response.AllResponses) {
                if (!tagEnumerator.MoveNext()) {
                    break;
                }

                var tag = tagEnumerator.Current;
                if (tag == null) {
                    continue;
                }

                var r = item as SearchResponse<TagValueDocument>;

                result[tag.Name] = new TagValueCollection() {
                    VisualizationHint = TagValueCollectionVisualizationHint.TrailingEdge,
                    Values = r.Aggs
                              .DateHistogram("aika")
                              .Buckets
                              .Select(x => new TagValue(x.Date, x.Average("avg_value").Value ?? Double.NaN, null, TagValueQuality.Good, tag.Units))
                              .ToArray()
                };
            }

            return result;
        }


        private async Task<IDictionary<string, TagValueCollection>> ReadMinimumDataInternal(IEnumerable<ElasticsearchTagDefinition> tags, string utcStartTimeAsString, DateTime utcStartTimeAsDate, string utcEndTimeAsString, DateTime utcEndTimeAsDate, TimeSpan sampleInterval, CancellationToken cancellationToken) {
            var result = new ConcurrentDictionary<string, TagValueCollection>();

            var multiSearchRequest = new MultiSearchDescriptor();
            foreach (var tag in tags) {
                multiSearchRequest = multiSearchRequest
                    .Search<TagValueDocument>(
                        selector => AddAggregatedDataQuerySearchDescriptor(selector, tag.IdAsGuid, utcStartTimeAsString, utcEndTimeAsString)
                            .Aggregations(
                                aa => aa.DateHistogram(
                                    "aika",
                                    dh => dh.Field(
                                        x => x.UtcSampleTime
                                    )
                                    .Interval(sampleInterval)
                                    .MinimumDocumentCount(0)
                                    .ExtendedBoundsDateMath(utcStartTimeAsDate, utcEndTimeAsDate)
                                    .Aggregations(
                                        aa2 => aa2.Min(
                                            "min_value",
                                            avg => avg.Field(
                                                x => x.NumericValue
                                            )
                                        )
                                    )
                                )
                            ));
            }

            var response = await Client.MultiSearchAsync(multiSearchRequest, cancellationToken).ConfigureAwait(false);

            var tagEnumerator = tags.GetEnumerator();
            foreach (var item in response.AllResponses) {
                if (!tagEnumerator.MoveNext()) {
                    break;
                }

                var tag = tagEnumerator.Current;
                if (tag == null) {
                    continue;
                }

                var r = item as SearchResponse<TagValueDocument>;

                result[tag.Name] = new TagValueCollection() {
                    VisualizationHint = TagValueCollectionVisualizationHint.TrailingEdge,
                    Values = r.Aggs
                              .DateHistogram("aika")
                              .Buckets
                              .Select(x => new TagValue(x.Date, x.Average("min_value").Value ?? Double.NaN, null, TagValueQuality.Good, tag.Units))
                              .ToArray()
                };
            }

            return result;
        }


        private async Task<IDictionary<string, TagValueCollection>> ReadMaximumDataInternal(IEnumerable<ElasticsearchTagDefinition> tags, string utcStartTimeAsString, DateTime utcStartTimeAsDate, string utcEndTimeAsString, DateTime utcEndTimeAsDate, TimeSpan sampleInterval, CancellationToken cancellationToken) {
            var result = new ConcurrentDictionary<string, TagValueCollection>();

            var multiSearchRequest = new MultiSearchDescriptor();
            foreach (var tag in tags) {
                multiSearchRequest = multiSearchRequest
                    .Search<TagValueDocument>(
                        selector => AddAggregatedDataQuerySearchDescriptor(selector, tag.IdAsGuid, utcStartTimeAsString, utcEndTimeAsString)
                            .Aggregations(
                                aa => aa.DateHistogram(
                                    "aika",
                                    dh => dh.Field(
                                        x => x.UtcSampleTime
                                    )
                                    .Interval(sampleInterval)
                                    .MinimumDocumentCount(0)
                                    .ExtendedBoundsDateMath(utcStartTimeAsDate, utcEndTimeAsDate)
                                    .Aggregations(
                                        aa2 => aa2.Min(
                                            "max_value",
                                            avg => avg.Field(
                                                x => x.NumericValue
                                            )
                                        )
                                    )
                                )
                            ));
            }

            var response = await Client.MultiSearchAsync(multiSearchRequest, cancellationToken).ConfigureAwait(false);

            var tagEnumerator = tags.GetEnumerator();
            foreach (var item in response.AllResponses) {
                if (!tagEnumerator.MoveNext()) {
                    break;
                }

                var tag = tagEnumerator.Current;
                if (tag == null) {
                    continue;
                }

                var r = item as SearchResponse<TagValueDocument>;

                result[tag.Name] = new TagValueCollection() {
                    VisualizationHint = TagValueCollectionVisualizationHint.TrailingEdge,
                    Values = r.Aggs
                              .DateHistogram("aika")
                              .Buckets
                              .Select(x => new TagValue(x.Date, x.Average("max_value").Value ?? Double.NaN, null, TagValueQuality.Good, tag.Units))
                              .ToArray()
                };
            }

            return result;
        }


        private async Task<IDictionary<string, TagValueCollection>> ReadInterpolatedDataInternal(IEnumerable<ElasticsearchTagDefinition> tags, string utcStartTimeAsString, DateTime utcStartTimeAsDate, string utcEndTimeAsString, DateTime utcEndTimeAsDate, TimeSpan bucketSize, CancellationToken cancellationToken) {
            var result = new ConcurrentDictionary<string, TagValueCollection>();

            var multiSearchRequest = new MultiSearchDescriptor();
            foreach (var tag in tags) {
                multiSearchRequest = multiSearchRequest
                    .Search<TagValueDocument>(
                        selector => AddAggregatedDataQuerySearchDescriptor(selector, tag.IdAsGuid, utcStartTimeAsString, utcEndTimeAsString)
                            .Aggregations(
                                aa => aa.DateHistogram(
                                    "aika",
                                    dh => dh.Field(
                                        x => x.UtcSampleTime
                                    )
                                    .Interval(bucketSize)
                                    .MinimumDocumentCount(0)
                                    .ExtendedBoundsDateMath(utcStartTimeAsDate, utcEndTimeAsDate)
                                    .Aggregations(
                                        aa2 => aa2.TopHits(
                                            "earliest_value",
                                            th => th.Sort(
                                                s => s.Ascending(
                                                    x => x.UtcSampleTime
                                                )
                                            )
                                            .Size(1)
                                            .Source(
                                                s => s.Includes(
                                                    i => i.Fields(
                                                        x => x.UtcSampleTime,
                                                        x => x.NumericValue,
                                                        x => x.Quality
                                                    )
                                                )
                                            )
                                        ).TopHits(
                                            "latest_value",
                                            th => th.Sort(
                                                s => s.Descending(
                                                    x => x.NumericValue
                                                )
                                            )
                                            .Size(1)
                                            .Source(
                                                s => s.Includes(
                                                    i => i.Fields(
                                                        x => x.UtcSampleTime,
                                                        x => x.NumericValue,
                                                        x => x.Quality
                                                    )
                                                )
                                            )
                                        )
                                    )
                                )
                        )
                    )
                    .Search<TagValueDocument>(selector => AddPreviousSampleSearchDescriptor(selector, tag.IdAsGuid, utcStartTimeAsString))
                    .Search<TagValueDocument>(selector => AddNextSampleSearchDescriptor(selector, tag.IdAsGuid, utcStartTimeAsString));
            }

            var response = await Client.MultiSearchAsync(multiSearchRequest, cancellationToken).ConfigureAwait(false);

            var responsesEnumerator = response.AllResponses.GetEnumerator();
            foreach (var tag in tags) {
                SearchResponse<TagValueDocument> queryResponse;
                SearchResponse<TagValueDocument> previousValueResponse;
                SearchResponse<TagValueDocument> nextValueResponse;

                if (!responsesEnumerator.MoveNext()) {
                    break;
                }
                queryResponse = responsesEnumerator.Current as SearchResponse<TagValueDocument>;

                if (!responsesEnumerator.MoveNext()) {
                    break;
                }
                previousValueResponse = responsesEnumerator.Current as SearchResponse<TagValueDocument>;

                if (!responsesEnumerator.MoveNext()) {
                    break;
                }
                nextValueResponse = responsesEnumerator.Current as SearchResponse<TagValueDocument>;

                var responseValues = new List<TagValueDocument>();
                var resultValues = new List<TagValue>();

                var hit = previousValueResponse.Documents.FirstOrDefault();
                if (hit != null) {
                    responseValues.Add(hit);
                }

                foreach (var bucket in queryResponse.Aggs.DateHistogram("aika").Buckets) {
                    hit = bucket.TopHits("earliest_value").Hits<TagValueDocument>().FirstOrDefault()?.Source;
                    if (hit != null) {
                        responseValues.Add(hit);
                    }

                    hit = bucket.TopHits("latest_value").Hits<TagValueDocument>().FirstOrDefault()?.Source;
                    if (hit != null) {
                        responseValues.Add(hit);
                    }
                }

                hit = nextValueResponse.Documents.FirstOrDefault();
                if (hit != null) {
                    responseValues.Add(hit);
                }

                var enumerator = responseValues.GetEnumerator();

                TagValueDocument value0 = null;
                TagValueDocument value1 = null;

                for (var sampleTime = utcStartTimeAsDate; sampleTime <= utcEndTimeAsDate; sampleTime = sampleTime.Add(bucketSize)) {
                    if (value0 != null && value0.UtcSampleTime == sampleTime) {
                        resultValues.Add(value0.ToTagValue(units: tag.Units));
                        continue;
                    }
                    if (value1 != null && value1.UtcSampleTime == sampleTime) {
                        resultValues.Add(value1.ToTagValue(units: tag.Units));
                        continue;
                    }
                    if (value0 != null && value1 != null && sampleTime > value0.UtcSampleTime && sampleTime < value1.UtcSampleTime) {
                        if (Double.IsNaN(value0.NumericValue) || Double.IsNaN(value1.NumericValue)) {
                            resultValues.Add(new TagValue(sampleTime, Double.NaN, null, new[] { value0.Quality, value1.Quality }.Min(), tag.Units));
                        }
                        else {
                            resultValues.Add(new TagValue(sampleTime, InterpolateValue(value0.UtcSampleTime.Ticks, value0.NumericValue, value1.UtcSampleTime.Ticks, value1.NumericValue, sampleTime.Ticks), null, new[] { value0.Quality, value1.Quality }.Min(), tag.Units));
                        }
                        continue;
                    }

                    value1 = enumerator.Current;

                    var @continue = true;

                    while (value1 == null || value1.UtcSampleTime < sampleTime) {
                        if (!enumerator.MoveNext()) {
                            @continue = false;
                            break;
                        }

                        if (!@continue) {
                            break;
                        }

                        value0 = value1;
                        value1 = enumerator.Current;
                    }

                    if (value0 != null && value1 != null && sampleTime >= value0.UtcSampleTime && sampleTime <= value1.UtcSampleTime) {
                        if (value0 != null && value0.UtcSampleTime == sampleTime) {
                            resultValues.Add(value0.ToTagValue(units: tag.Units));
                            continue;
                        }
                        if (value1 != null && value1.UtcSampleTime == sampleTime) {
                            resultValues.Add(value1.ToTagValue(units: tag.Units));
                            continue;
                        }

                        if (Double.IsNaN(value0.NumericValue) || Double.IsNaN(value1.NumericValue)) {
                            resultValues.Add(new TagValue(sampleTime, Double.NaN, null, new[] { value0.Quality, value1.Quality }.Min(), tag.Units));
                        }
                        else {
                            resultValues.Add(new TagValue(sampleTime, InterpolateValue(value0.UtcSampleTime.Ticks, value0.NumericValue, value1.UtcSampleTime.Ticks, value1.NumericValue, sampleTime.Ticks), null, new[] { value0.Quality, value1.Quality }.Min(), tag.Units));
                        }
                    }
                }

                result[tag.Name] = new TagValueCollection() {
                    VisualizationHint = TagValueCollectionVisualizationHint.Interpolated,
                    Values = resultValues
                };
            }

            return result;
        }


        public override async Task<IDictionary<string, TagValueCollection>> ReadPlotData(ClaimsPrincipal identity, IEnumerable<string> tagNames, DateTime utcStartTime, DateTime utcEndTime, int intervals, CancellationToken cancellationToken) {
            if (tagNames == null) {
                return new Dictionary<string, TagValueCollection>();
            }

            var result = new ConcurrentDictionary<string, TagValueCollection>();
            var tags = GetTagsByIdOrName(identity, tagNames);

            var numericTags = tags.Where(x => x.DataType == TagDataType.FloatingPoint || x.DataType == TagDataType.Integer).ToArray();
            var nonNumericTags = tags.Except(numericTags);

            var gte = GetIsoDateString(utcStartTime);
            var lte = GetIsoDateString(utcEndTime);

            if (intervals < 1) {
                intervals = 1;
            }

            var bucketSize = utcStartTime == utcEndTime 
                ? TimeSpan.FromSeconds(1) 
                : TimeSpan.FromMilliseconds((utcEndTime - utcStartTime).TotalMilliseconds / intervals);

            var estimatedPointCount = (int) Math.Ceiling(bucketSize.TotalMilliseconds / intervals) * 5;

            var take = estimatedPointCount > MaxSamplesPerTagPerQuery
                ? MaxSamplesPerTagPerQuery
                : estimatedPointCount;

            var tagGroups = GetTagQueryGroups(tags, take);

            var queries = tagGroups.Select(grp => ReadPlotDataInternal(grp, gte, utcStartTime, lte, utcEndTime, bucketSize, estimatedPointCount, cancellationToken).ContinueWith(t => {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var item in t.Result) {
                    result[item.Key] = item.Value;
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion)).ToArray();

            await Task.WhenAny(Task.WhenAll(queries), Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            return result;
        }


        private async Task<IDictionary<string, TagValueCollection>> ReadPlotDataInternal(IEnumerable<ElasticsearchTagDefinition> tags, string utcStartTimeAsString, DateTime utcStartTimeAsDate, string utcEndTimeAsString, DateTime utcEndTimeAsDate, TimeSpan bucketSize, int nonNumericPointCount, CancellationToken cancellationToken) {
            var result = new ConcurrentDictionary<string, TagValueCollection>();

            var multiSearchRequest = new MultiSearchDescriptor();
            foreach (var tag in tags) {
                if (tag.DataType == TagDataType.FloatingPoint || tag.DataType == TagDataType.Integer) {
                    multiSearchRequest = multiSearchRequest
                        .Search<TagValueDocument>(
                            selector => AddAggregatedDataQuerySearchDescriptor(selector, tag.IdAsGuid, utcStartTimeAsString, utcEndTimeAsString)
                                .Aggregations(
                                    aa => aa.DateHistogram(
                                        "aika",
                                        dh => dh.Field(
                                            x => x.UtcSampleTime
                                        )
                                        .Interval(bucketSize)
                                        .MinimumDocumentCount(0)
                                        .ExtendedBoundsDateMath(utcStartTimeAsDate, utcEndTimeAsDate)
                                        .Aggregations(
                                            aa2 => aa2.TopHits(
                                                "min_value",
                                                th => th.Sort(
                                                    s => s.Ascending(
                                                        x => x.NumericValue
                                                    )
                                                )
                                                .Size(1)
                                                .Source(
                                                    s => s.Includes(
                                                        i => i.Fields(
                                                            x => x.UtcSampleTime,
                                                            x => x.NumericValue,
                                                            x => x.Quality
                                                        )
                                                    )
                                                )
                                            ).TopHits(
                                                "max_value",
                                                th => th.Sort(
                                                    s => s.Descending(
                                                        x => x.NumericValue
                                                    )
                                                )
                                                .Size(1)
                                                .Source(
                                                    s => s.Includes(
                                                        i => i.Fields(
                                                            x => x.UtcSampleTime,
                                                            x => x.NumericValue,
                                                            x => x.Quality
                                                        )
                                                    )
                                                )
                                            ).TopHits(
                                                "earliest_value",
                                                th => th.Sort(
                                                    s => s.Ascending(
                                                        x => x.UtcSampleTime
                                                    )
                                                )
                                                .Size(1)
                                                .Source(
                                                    s => s.Includes(
                                                        i => i.Fields(
                                                            x => x.UtcSampleTime,
                                                            x => x.NumericValue,
                                                            x => x.Quality
                                                        )
                                                    )
                                                )
                                            ).TopHits(
                                                "latest_value",
                                                th => th.Sort(
                                                    s => s.Descending(
                                                        x => x.NumericValue
                                                    )
                                                )
                                                .Size(1)
                                                .Source(
                                                    s => s.Includes(
                                                        i => i.Fields(
                                                            x => x.UtcSampleTime,
                                                            x => x.NumericValue,
                                                            x => x.Quality
                                                        )
                                                    )
                                                )
                                            ).TopHits(
                                                "quality_value",
                                                th => th.Sort(
                                                    s => s.Ascending(
                                                        x => x.Quality
                                                    )
                                                )
                                                .Size(1)
                                                .Source(
                                                    s => s.Includes(
                                                        i => i.Fields(
                                                            x => x.UtcSampleTime,
                                                            x => x.NumericValue,
                                                            x => x.Quality
                                                        )
                                                    )
                                                )
                                            )
                                        )
                                    )
                            )
                        )
                        .Search<TagValueDocument>(selector => AddPreviousSampleSearchDescriptor(selector, tag.IdAsGuid, utcStartTimeAsString))
                        .Search<TagValueDocument>(selector => AddNextSampleSearchDescriptor(selector, tag.IdAsGuid, utcStartTimeAsString));
                }
                else {
                    multiSearchRequest = multiSearchRequest
                        .Search<TagValueDocument>(selector => AddRawDataQuerySearchDescriptor(selector, tag.IdAsGuid, utcStartTimeAsString, utcEndTimeAsString, nonNumericPointCount))
                        .Search<TagValueDocument>(selector => AddPreviousSampleSearchDescriptor(selector, tag.IdAsGuid, utcStartTimeAsString))
                        .Search<TagValueDocument>(selector => AddNextSampleSearchDescriptor(selector, tag.IdAsGuid, utcStartTimeAsString));
                }
            }

            if (Logger?.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace) ?? false) {
                var json = Client.Serializer.SerializeToString(multiSearchRequest);
                Logger.LogTrace($"PLOT Query: {json}");
            }

            var response = await Client.MultiSearchAsync(multiSearchRequest, cancellationToken).ConfigureAwait(false);

            var responsesEnumerator = response.AllResponses.GetEnumerator();
            foreach (var tag in tags) {
                SearchResponse<TagValueDocument> queryResponse;
                SearchResponse<TagValueDocument> previousValueResponse;
                SearchResponse<TagValueDocument> nextValueResponse;

                if (!responsesEnumerator.MoveNext()) {
                    break;
                }
                queryResponse = responsesEnumerator.Current as SearchResponse<TagValueDocument>;

                if (!responsesEnumerator.MoveNext()) {
                    break;
                }
                previousValueResponse = responsesEnumerator.Current as SearchResponse<TagValueDocument>;

                if (!responsesEnumerator.MoveNext()) {
                    break;
                }
                nextValueResponse = responsesEnumerator.Current as SearchResponse<TagValueDocument>;

                if (tag.DataType == TagDataType.FloatingPoint || tag.DataType == TagDataType.Integer) {
                    var values = new List<TagValue>();

                    foreach (var bucket in queryResponse.Aggs.DateHistogram("aika").Buckets) {
                        var bucketVals = new List<TagValue>();

                        foreach (var hit in bucket.TopHits("min_value").Hits<TagValueDocument>()) {
                            bucketVals.Add(hit.Source.ToTagValue(units: tag.Units));
                        }
                        foreach (var hit in bucket.TopHits("max_value").Hits<TagValueDocument>()) {
                            bucketVals.Add(hit.Source.ToTagValue(units: tag.Units));
                        }
                        foreach (var hit in bucket.TopHits("earliest_value").Hits<TagValueDocument>()) {
                            bucketVals.Add(hit.Source.ToTagValue(units: tag.Units));
                        }
                        foreach (var hit in bucket.TopHits("latest_value").Hits<TagValueDocument>()) {
                            bucketVals.Add(hit.Source.ToTagValue(units: tag.Units));
                        }
                        foreach (var hit in bucket.TopHits("quality_value").Hits<TagValueDocument>()) {
                            if (hit.Source.Quality == TagValueQuality.Good) {
                                continue;
                            }
                            bucketVals.Add(hit.Source.ToTagValue(units: tag.Units));
                        }

                        values.AddRange(bucketVals.Distinct().OrderBy(x => x.UtcSampleTime));
                    }

                    var first = values.FirstOrDefault();
                    if (first != null && first.UtcSampleTime > utcStartTimeAsDate) {
                        var prev = previousValueResponse.Documents.FirstOrDefault();
                        if (prev != null && !Double.IsNaN(prev.NumericValue)) {
                            values.Insert(0, new TagValue(utcStartTimeAsDate, InterpolateValue(prev.UtcSampleTime.Ticks, prev.NumericValue, first.UtcSampleTime.Ticks, first.NumericValue, utcStartTimeAsDate.Ticks), null, prev.Quality, tag.Units));
                        }
                    }

                    var last = values.LastOrDefault();
                    if (last != null && last.UtcSampleTime < utcEndTimeAsDate) {
                        var next = nextValueResponse.Documents.FirstOrDefault();
                        if (next != null && !Double.IsNaN(next.NumericValue)) {
                            values.Add(new TagValue(utcEndTimeAsDate, InterpolateValue(last.UtcSampleTime.Ticks, last.NumericValue, next.UtcSampleTime.Ticks, next.NumericValue, utcEndTimeAsDate.Ticks), null, next.Quality, tag.Units));
                        }
                    }

                    result[tag.Name] = new TagValueCollection() {
                        VisualizationHint = TagValueCollectionVisualizationHint.Interpolated,
                        Values = values
                    };
                }
                else {
                    var values = queryResponse.Documents.Select(x => x.ToTagValue(units: tag.Units)).ToList();

                    var first = values.FirstOrDefault();
                    if (first != null && first.UtcSampleTime > utcStartTimeAsDate) {
                        var prev = previousValueResponse.Documents.FirstOrDefault();
                        if (prev != null) {
                            values.Insert(0, prev.ToTagValue(utcSampleTime: utcStartTimeAsDate, units: tag.Units));
                        }
                    }

                    var last = values.LastOrDefault();
                    if (last != null && last.UtcSampleTime < utcEndTimeAsDate) {
                        var next = nextValueResponse.Documents.FirstOrDefault();
                        if (next != null) {
                            values.Add(next.ToTagValue(utcSampleTime: utcEndTimeAsDate, units: tag.Units));
                        }
                    }

                    result[tag.Name] = new TagValueCollection() {
                        VisualizationHint = TagValueCollectionVisualizationHint.TrailingEdge,
                        Values = values
                    };
                }
            }

            return result;
        }


        private static double InterpolateValue(double x0, double y0, double x1, double y1, double x) {
            return ((y0 * (x1 - x)) + (y1 * (x - x0))) / (x1 - x0);
        }


        #endregion

        #region [ Write Tag Data ]

        public override Task<IDictionary<string, bool>> CanWriteTagData(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            var result = tagNames.ToDictionary(x => x, x => true);
            return Task.FromResult<IDictionary<string, bool>>(result);
        }


        internal Task WriteSnapshotValue(ElasticsearchTagDefinition tag, TagValue value, CancellationToken cancellationToken) {
            _snapshotWriter.WriteValue(tag.IdAsGuid, value);
            return Task.CompletedTask;
        }


        internal async Task<WriteTagValuesResult> InsertArchiveValues(ElasticsearchTagDefinition tag, IEnumerable<TagValue> values, TagValue nextArchiveCandidate, CancellationToken cancellationToken) {
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

        #endregion

    }
}
