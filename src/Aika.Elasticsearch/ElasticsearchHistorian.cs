using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Aika.Elasticsearch.Documents;
using Aika.StateSets;
using Aika.Tags;
using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Nest;

namespace Aika.Elasticsearch {

    /// <summary>
    /// Aika historian that uses Elasticsearch for storage and aggregation.
    /// </summary>
    public class ElasticsearchHistorian : HistorianBase {

        #region [ Fields / Properties ]

        /// <summary>
        /// The default prefix for Elasticsearch indices.
        /// </summary>
        public const string DefaultIndexPrefix = "aika-";

        /// <summary>
        /// The actual Elasticsearch index prefix used at runtime.
        /// </summary>
        private readonly string _indexPrefix;

        /// <summary>
        /// Gets the name of the tags index for the historian.
        /// </summary>
        public string TagsIndexName { get; }

        /// <summary>
        /// Gets the name of the tag change history index for the historian.
        /// </summary>
        public string TagChangeHistoryIndexName { get; }

        /// <summary>
        /// Gets the name of the state sets index for the historian.
        /// </summary>
        public string StateSetsIndexName { get; }

        /// <summary>
        /// Gets the name of the snapshot value index for the historian.
        /// </summary>
        public string SnapshotValuesIndexName { get; }

        /// <summary>
        /// Gets the name of the archive candidates index for the historian.
        /// </summary>
        public string ArchiveCandidatesIndexName { get; }

        /// <summary>
        /// Gets the prefix for the historian's archive indices.
        /// </summary>
        public string ArchiveIndexNamePrefix { get; }

        /// <summary>
        /// A function for generating the archive index name suffix to use for a tag and sample time 
        /// combination.
        /// </summary>
        internal ArchiveIndexNameSuffixGenerator ArchiveIndexSuffixGenerator { get; }

        /// <summary>
        /// An index descriptor that is used to search across permanently-archived, archive candidate, 
        /// and snapshot tag values.
        /// </summary>
        private readonly string _dataQueryIndices;

        /// <summary>
        /// The maximum number of samples that will be requested per tag per Elasticsearch query.
        /// </summary>
        private const int MaxSamplesPerTagPerQuery = 5000;

        /// <summary>
        /// The overall maximum number of samples that will be requested per Elasticsearch query.
        /// </summary>
        private const int MaxSamplesPerQuery = 20000;

        /// <summary>
        /// The maximum number of tags that will be included in a single Elasticsearch query.
        /// </summary>
        private const int MaxTagsPerQuery = 100;

        /// <summary>
        /// ISO date format to use when converting <see cref="DateTime"/> to <see cref="String"/> (e.g. 
        /// for use in Elasticsearch data queries).
        /// </summary>
        private const string IsoDateFormat = "yyyy-MM-ddTHH:mm:ss.fffffffZ";

        /// <summary>
        /// Gets the historian description.
        /// </summary>
        public override string Description {
            get { return Resources.ElasticHistorian_Description; }
        }

        /// <summary>
        /// Gets the <see cref="ITaskRunner"/> for the historian.
        /// </summary>
        internal new ITaskRunner TaskRunner {
            get { return base.TaskRunner; }
        }

        /// <summary>
        /// Gets the NEST client for querying Elasticsearch.
        /// </summary>
        internal ElasticClient Client { get; }

        /// <summary>
        /// Holds tag definitions, indexed by ID.
        /// </summary>
        private readonly ConcurrentDictionary<string, ElasticsearchTagDefinition> _tagsById = new ConcurrentDictionary<string, ElasticsearchTagDefinition>();

        /// <summary>
        /// Holds tag definitions, indexed by name.
        /// </summary>
        private readonly ConcurrentDictionary<string, ElasticsearchTagDefinition> _tagsByName = new ConcurrentDictionary<string, ElasticsearchTagDefinition>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Holds tag state sets, indexed by name,
        /// </summary>
        private readonly ConcurrentDictionary<string, StateSet> _stateSets = new ConcurrentDictionary<string, StateSet>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Holds the archive data index names that are known to exist.
        /// </summary>
        private readonly ConcurrentDictionary<string, object> _archiveIndices = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Handles background writing of snapshot values to Elasticsearch.
        /// </summary>
        private BackgroundSnapshotValuesWriter _snapshotWriter;

        /// <summary>
        /// Handles background writing of archive values to Elasticsearch.
        /// </summary>
        private BackgroundArchiveValuesWriter _archiveWriter;

        /// <summary>
        /// The data query functions supported by the historian.
        /// </summary>
        private static readonly DataQueryFunction[] DataQueryFunctions = {
            new DataQueryFunction(DataQueryFunction.Average, "Average value calculated over a sample interval."),
            new DataQueryFunction(DataQueryFunction.Interpolated, "Interpolated data."),
            new DataQueryFunction(DataQueryFunction.Minimum, "Minimum value over a sample interval."),
            new DataQueryFunction(DataQueryFunction.Maximum, "Maximum value over a sample interval.")
        };

        #endregion

        #region [ Constructor ]

        /// <summary>
        /// Creates a new <see cref="ElasticsearchHistorian"/> object.
        /// </summary>
        /// <param name="client">The NEST client to use for Elasticsearch queries.</param>
        /// <param name="options">The historian options.</param>
        /// <param name="taskRunner">The <see cref="ITaskRunner"/> to use for running background tasks.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use.</param>
        public ElasticsearchHistorian(ElasticClient client, Options options, ITaskRunner taskRunner, ILoggerFactory loggerFactory) : base(taskRunner, loggerFactory) {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            _indexPrefix = String.IsNullOrWhiteSpace(options?.IndexPrefix)
                ? DefaultIndexPrefix
                : options.IndexPrefix.Trim();
            ArchiveIndexSuffixGenerator = options?.GetArchiveIndexSuffix;

            TagsIndexName = _indexPrefix + "tags";
            TagChangeHistoryIndexName = _indexPrefix + "tag-config-history";
            StateSetsIndexName = _indexPrefix + "state-sets";
            SnapshotValuesIndexName = _indexPrefix + "snapshot";
            ArchiveCandidatesIndexName = _indexPrefix + "archive-temporary";
            ArchiveIndexNamePrefix = _indexPrefix + "archive-permanent-";
            _dataQueryIndices = ArchiveIndexNamePrefix + "*," + ArchiveCandidatesIndexName + "," + SnapshotValuesIndexName;
        }

        #endregion

        #region [ Initialization and Elasticsearch Index Management ]

        /// <summary>
        /// Initializes the historian.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will initialize the historian.
        /// </returns>
        protected override async Task Init(CancellationToken cancellationToken) {
            _snapshotWriter?.Dispose();
            _archiveWriter?.Dispose();

            await CreateFixedIndices(cancellationToken).ConfigureAwait(false);
            await Task.WhenAll(
                LoadArchiveIndexNames(cancellationToken),
                LoadStateSetsFromElasticsearch(cancellationToken)
            ).ConfigureAwait(false);
            await LoadTagsFromElasticsearch(cancellationToken).ConfigureAwait(false);

            _snapshotWriter = new BackgroundSnapshotValuesWriter(this, TimeSpan.FromSeconds(2), LoggerFactory);
            _archiveWriter = new BackgroundArchiveValuesWriter(this, TimeSpan.FromSeconds(2), LoggerFactory);

            TaskRunner.RunBackgroundTask(ct => _snapshotWriter.Execute(ct));
            TaskRunner.RunBackgroundTask(ct => _archiveWriter.Execute(ct));
        }


        /// <summary>
        /// Ensures that the indices with fixed names (i.e. everything except archive data) exist in Elasticsearch.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will create the fixed indices if required.
        /// </returns>
        private async Task CreateFixedIndices(CancellationToken cancellationToken) {
            // Make sure that the fixed indices (tag definitions, change history, state sets, snapshot values, and archive candidate values) exist.
            var existsResponse = await Client.IndexExistsAsync(new IndexExistsRequest(TagsIndexName), cancellationToken).ConfigureAwait(false);
            if (!existsResponse.Exists) {
                await Client.CreateIndexAsync(IndexUtility.GetTagsIndexDescriptor(TagsIndexName), cancellationToken).ConfigureAwait(false);
            }

            existsResponse = await Client.IndexExistsAsync(new IndexExistsRequest(TagChangeHistoryIndexName), cancellationToken).ConfigureAwait(false);
            if (!existsResponse.Exists) {
                await Client.CreateIndexAsync(IndexUtility.GetTagsIndexDescriptor(TagChangeHistoryIndexName), cancellationToken).ConfigureAwait(false);
            }

            existsResponse = await Client.IndexExistsAsync(new IndexExistsRequest(StateSetsIndexName), cancellationToken).ConfigureAwait(false);
            if (!existsResponse.Exists) {
                await Client.CreateIndexAsync(IndexUtility.GetStateSetsIndexDescriptor(StateSetsIndexName), cancellationToken).ConfigureAwait(false);
            }

            existsResponse = await Client.IndexExistsAsync(new IndexExistsRequest(SnapshotValuesIndexName), cancellationToken).ConfigureAwait(false);
            if (!existsResponse.Exists) {
                await Client.CreateIndexAsync(IndexUtility.GetSnapshotOrArchiveCandidateValuesIndexDescriptor(SnapshotValuesIndexName), cancellationToken).ConfigureAwait(false);
            }

            existsResponse = await Client.IndexExistsAsync(new IndexExistsRequest(ArchiveCandidatesIndexName), cancellationToken).ConfigureAwait(false);
            if (!existsResponse.Exists) {
                await Client.CreateIndexAsync(IndexUtility.GetSnapshotOrArchiveCandidateValuesIndexDescriptor(ArchiveCandidatesIndexName), cancellationToken).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Gets the names of the archive data indices that have been created.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will get the archive data index names and assign them to <see cref="_archiveIndices"/>.
        /// </returns>
        private async Task LoadArchiveIndexNames(CancellationToken cancellationToken) {
            _archiveIndices.Clear();
            var response = await Client.GetIndexAsync(ArchiveIndexNamePrefix + "*", null, cancellationToken).ConfigureAwait(false);
            foreach (var item in response.Indices) {
                _archiveIndices[item.Key] = new object();
            }
        }


        /// <summary>
        /// Creates an archive data index for storing data for a tag with the specified sample time, if 
        /// required.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="utcSampleTime">The UTC sample time of the value that will be archived.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will create the required Elasticsearch index if it does not already exists.
        /// </returns>
        private async Task CreateArchiveIndex(ElasticsearchTagDefinition tag, DateTime utcSampleTime, CancellationToken cancellationToken) {
            var indexName = IndexUtility.GetIndexNameForArchiveTagValue(ArchiveIndexNamePrefix, tag, utcSampleTime, ArchiveIndexSuffixGenerator);
            if (_archiveIndices.ContainsKey(indexName)) {
                return;
            }

            var response = await Client.CreateIndexAsync(IndexUtility.GetArchiveValuesIndexDescriptor(indexName), cancellationToken).ConfigureAwait(false);
            if (response.IsValid) {
                _archiveIndices[indexName] = new object();
            }
        }


        /// <summary>
        /// Loads all state set definitions from Elasticsearch.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will load the state sets from Elasticsearch.
        /// </returns>
        private async Task LoadStateSetsFromElasticsearch(CancellationToken cancellationToken) {
            const int pageSize = 100;
            var page = 0;

            _stateSets.Clear();

            var @continue = false;
            do {
                ++page;
                var results = await Client.SearchAsync<StateSetDocument>(x => x.Index(StateSetsIndexName).From((page - 1) * pageSize).Size(pageSize), cancellationToken).ConfigureAwait(false);
                @continue = results.Hits.Count == pageSize;

                if (results.Hits.Count > 0) {
                    foreach (var hit in results.Hits) {
                        var stateSet = hit.Source.ToStateSet();
                        _stateSets[stateSet.Name] = stateSet;
                    }
                }
            } while (@continue);
        }


        /// <summary>
        /// Loads all tag definitions from Elasticsearch.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will load tag definitions from Elasticsearch and initialize them.
        /// </returns>
        private async Task LoadTagsFromElasticsearch(CancellationToken cancellationToken) {
            const int pageSize = 100;
            var page = 0;

            var snapshotValuesTask = LoadSnapshotOrArchiveCandidateValuesFromElasticsearch(SnapshotValuesIndexName, cancellationToken);
            var archiveCandidateValuesTask = LoadSnapshotOrArchiveCandidateValuesFromElasticsearch(ArchiveCandidatesIndexName, cancellationToken);

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

                            var archiveCandidateActual = archiveCandidate?.ToArchiveCandidateValue(settings.Units) ?? null;

                            var tag = new ElasticsearchTagDefinition(this,
                                                               hit.Source.Id,
                                                               settings,
                                                               metadata,
                                                               hit.Source.Security.ToTagSecurity(),
                                                               new InitialTagValues(snapshot?.ToTagValue(units: settings.Units), lastArchived?.ToTagValue(units: settings.Units), archiveCandidateActual?.Value, archiveCandidateActual?.CompressionAngleMinimum ?? Double.NaN, archiveCandidateActual?.CompressionAngleMaximum ?? Double.NaN),
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


        /// <summary>
        /// Loads snapshot or archive candidate values for all tags fro Elasticsearch.
        /// </summary>
        /// <param name="indexName">The index to load data from.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the values indexed by tag ID.
        /// </returns>
        private async Task<IDictionary<Guid, TagValueDocument>> LoadSnapshotOrArchiveCandidateValuesFromElasticsearch(string indexName, CancellationToken cancellationToken) {
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


        /// <summary>
        /// Loads the last-archived value for the specified tag ID.
        /// </summary>
        /// <param name="tagId">The tag ID.</param>
        /// <param name="indexNames">The index names to query for the last-archived value.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the last-archived value for the tag.
        /// </returns>
        private async Task<TagValueDocument> LoadLastArchivedValueFromElasticsearch(Guid tagId, string[] indexNames, CancellationToken cancellationToken) {
            foreach (var index in indexNames) {
                var response = await Client.SearchAsync<TagValueDocument>(
                    x => x.Index(index).Query(
                        q => q.Term(
                            t => t.TagId, tagId
                        )
                    )
                    .Sort(
                        s => s.Descending(
                            f => f.UtcSampleTime
                        )
                    )
                    .From(0)
                    .Size(1), 
                    cancellationToken
                ).ConfigureAwait(false);

                if (response.Hits.Count > 0) {
                    return response.Hits.First().Source;
                }
            }

            return null;
        }

        #endregion

        #region [ State Set Management ]

        /// <summary>
        /// Gets the state sets that match the specified filter.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the matching state sets.
        /// </returns>
        protected override Task<IEnumerable<StateSet>> GetStateSets(ClaimsPrincipal identity, StateSetFilter filter, CancellationToken cancellationToken) {
            IEnumerable<StateSet> result = _stateSets.Values;

            if (!String.IsNullOrWhiteSpace(filter?.Filter)) {
                result = result.Where(x => x.Name.Contains(filter.Filter));
            }

            var pageSize = filter?.PageSize ?? StateSetFilter.DefaultPageSize;
            var page = filter?.Page ?? 1;

            result = result.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize);

            return Task.FromResult<IEnumerable<StateSet>>(result);
        }


        /// <summary>
        /// Gets the state set with the specified name.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="name">The name of the state set.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The state set.
        /// </returns>
        protected override Task<StateSet> GetStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken) {
            if (String.IsNullOrWhiteSpace(name) || !_stateSets.TryGetValue(name, out var result)) {
                return Task.FromResult<StateSet>(null);
            }

            return Task.FromResult(result);
        }


        /// <summary>
        /// Creates a new state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="settings">The state set settings.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The new state set.
        /// </returns>
        protected override async Task<StateSet> CreateStateSet(ClaimsPrincipal identity, StateSetSettings settings, CancellationToken cancellationToken) {
            if (_stateSets.ContainsKey(settings.Name)) {
                throw new ArgumentException(Resources.Error_StateSetAlreadyExists, nameof(settings));
            }

            var stateSet = new StateSet(settings.Name, settings.Description, settings.States);
            await Client.IndexAsync(stateSet.ToStateSetDocument(), x => x.Index(StateSetsIndexName), cancellationToken).ConfigureAwait(false);

            _stateSets[stateSet.Name] = stateSet;
            return stateSet;
        }


        /// <summary>
        /// Updates a state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="name">The state set name.</param>
        /// <param name="settings">The updated state set settings.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The updated state set.
        /// </returns>
        protected override async Task<StateSet> UpdateStateSet(ClaimsPrincipal identity, string name, StateSetSettings settings, CancellationToken cancellationToken) {
            if (!_stateSets.TryGetValue(name, out var existing)) {
                throw new ArgumentException(Resources.Error_StateSetDoesNotExist, nameof(settings));
            }

            var newValue = new StateSet(existing.Name, settings.Description, settings.States);
            await Client.IndexAsync(newValue.ToStateSetDocument(), x => x.Index(StateSetsIndexName), cancellationToken).ConfigureAwait(false);

            _stateSets[newValue.Name] = newValue;
            return newValue;
        }


        /// <summary>
        /// Deletes a state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="name">The state set name.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A flag that indicates if the delete was successful.
        /// </returns>
        protected override async Task<bool> DeleteStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken) {
            if (!_stateSets.TryGetValue(name, out var value)) {
                return false;
            }

            await Client.DeleteAsync(new DocumentPath<StateSetDocument>(value.Name), x => x.Index(StateSetsIndexName), cancellationToken).ConfigureAwait(false);
            _stateSets.TryRemove(value.Name, out var _);
            return true;
        }

        #endregion

        #region [ Tag Management ]

        /// <summary>
        /// Performs a tag search.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="filter">The tag search filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the matching tags.
        /// </returns>
        protected override Task<IEnumerable<TagDefinition>> GetTags(ClaimsPrincipal identity, TagDefinitionFilter filter, CancellationToken cancellationToken) {
            var result = filter?.GetMatchingTags(_tagsById.Values, null) ?? new TagDefinition[0];
            return Task.FromResult(result);
        }


        /// <summary>
        /// Retrieves tag definitions by ID or name.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagIdsOrNames">The IDs or tag names to retrieve the tag definitions for.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary of tag definitions, indexed by the items in <paramref name="tagIdsOrNames"/>.
        /// </returns>
        protected override Task<IDictionary<string, TagDefinition>> GetTags(ClaimsPrincipal identity, IEnumerable<string> tagIdsOrNames, CancellationToken cancellationToken) {
            IDictionary<string, ElasticsearchTagDefinition> result;

            if (tagIdsOrNames == null) {
                result = new Dictionary<string, ElasticsearchTagDefinition>();
            }
            else {
                result = GetTagsByIdOrName(identity, tagIdsOrNames);
            }
            return Task.FromResult<IDictionary<string, TagDefinition>>(result.ToDictionary(x => x.Key, x => (TagDefinition) x.Value, StringComparer.OrdinalIgnoreCase));
        }


        /// <summary>
        /// Retrieves tag definitions by ID or name.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagIdsOrNames">The IDs or tag names to retrieve the tag definitions for.</param>
        /// <returns>
        /// A dictionary of tag definitions, indexed by the items in <paramref name="tagIdsOrNames"/>.
        /// </returns>
        private IDictionary<string, ElasticsearchTagDefinition> GetTagsByIdOrName(ClaimsPrincipal identity, IEnumerable<string> tagIdsOrNames) {
            if (tagIdsOrNames == null) {
                return new Dictionary<string, ElasticsearchTagDefinition>();
            }

            var result = new Dictionary<string, ElasticsearchTagDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in tagIdsOrNames) {
                if (_tagsById.TryGetValue(item, out var tag)) {
                    result[item] = tag;
                }
                else if (_tagsByName.TryGetValue(item, out tag)) {
                    result[item] = tag;
                }
            }

            return result;
        }


        /// <summary>
        /// Gets the specified tag definition.
        /// </summary>
        /// <param name="id">The ID of the tag.</param>
        /// <returns>The matching tag definition.</returns>
        internal ElasticsearchTagDefinition GetTagById(Guid id) {
            return _tagsById.TryGetValue(id.ToString(), out var tag) ? tag : null;
        }


        /// <summary>
        /// Creates a new tag.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tag">The tag settings.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The new tag definition.
        /// </returns>
        protected override async Task<TagDefinition> CreateTag(ClaimsPrincipal identity, TagSettings tag, CancellationToken cancellationToken) {
            var exists = _tagsByName.ContainsKey(tag.Name);
            if (exists) {
                throw new ArgumentException(Resources.Error_TagAlreadyExists, nameof(tag));
            }

            // TODO: allow policies to be passed through, or set default policies via a callback.
            // Allow read/write by everyone by default.
            string owner = null;
            if (identity != null) {
                var idClaim = identity.FindFirst(ClaimTypes.NameIdentifier);
                if (idClaim != null) {
                    owner = idClaim.Value;
                }
            }

            Tags.Security.TagSecurity security = new Tags.Security.TagSecurity(owner, new Dictionary<string, Tags.Security.TagSecurityPolicy>() {
                {
                    Tags.Security.TagSecurityPolicy.DataRead,
                    new Tags.Security.TagSecurityPolicy(new [] { new Tags.Security.TagSecurityEntry(null, "*") }, null)
                },
                {
                    Tags.Security.TagSecurityPolicy.DataWrite,
                    new Tags.Security.TagSecurityPolicy(new [] { new Tags.Security.TagSecurityEntry(null, "*") }, null)
                }
            });

            var metadata = new TagMetadata(DateTime.UtcNow, identity?.Identity?.Name);
            var tagDefinition = new ElasticsearchTagDefinition(this, Guid.NewGuid(), tag, metadata, security, null, null);

            await Client.IndexAsync(tagDefinition.ToTagDocument(), x => x.Index(TagsIndexName), cancellationToken).ConfigureAwait(false);

            _tagsById[tagDefinition.Id] = tagDefinition;
            _tagsById[tagDefinition.Name] = tagDefinition;
            return tagDefinition;
        }


        /// <summary>
        /// Updates a tag.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="update">The updated settings.</param>
        /// <param name="description">A summary description of the update.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The updated tag definition.
        /// </returns>
        protected override async Task<TagDefinition> UpdateTag(ClaimsPrincipal identity, TagDefinition tag, TagSettings update, string description, CancellationToken cancellationToken) {
            if (!_tagsById.TryGetValue(tag.Id, out var esTag)) {
                throw new ArgumentException(Resources.Error_TagDoesNotExist, nameof(tag));
            }

            if (!esTag.IsAuthorized(identity, Tags.Security.TagSecurityPolicy.Administrator)) {
                throw new SecurityException();
            }

            var nameChange = !String.IsNullOrWhiteSpace(update.Name) && !tag.Name.Equals(update.Name, StringComparison.OrdinalIgnoreCase);
            if (nameChange && _tagsByName.ContainsKey(update.Name)) {
                throw new ArgumentException(Resources.Error_TagAlreadyExists, nameof(update));
            }

            var oldConfig = esTag.ToTagDocument();

            var change = esTag.Update(update, identity, description);
            if (nameChange) {
                _tagsByName.TryRemove(oldConfig.Name, out var _);
                _tagsByName[tag.Name] = esTag;
            }

            await Task.WhenAll(Client.IndexAsync(esTag.ToTagDocument(), x => x.Index(TagsIndexName), cancellationToken),
                               Client.IndexAsync(new TagChangeHistoryDocument() {
                                   Id = change.Id,
                                   TagId = esTag.IdAsGuid,
                                   UtcTime = change.UtcTime,
                                   User = change.User,
                                   Description = change.Description,
                                   PreviousVersion = oldConfig
                               })).ConfigureAwait(false);

            return tag;
        }


        /// <summary>
        /// Deletes a tag.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagId">The tag ID.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A flag that indicates in the delete was successful.
        /// </returns>
        protected override async Task<bool> DeleteTag(ClaimsPrincipal identity, string tagId, CancellationToken cancellationToken) {
            if (!_tagsById.TryGetValue(tagId, out var tag)) {
                return false;
            }

            if (!tag.IsAuthorized(identity, Tags.Security.TagSecurityPolicy.Administrator)) {
                throw new SecurityException();
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

        /// <summary>
        /// Gets the data query functions supported by the historian.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The list of supported data query functions.
        /// </returns>
        protected override Task<IEnumerable<DataQueryFunction>> GetAvailableDataQueryFunctions(CancellationToken cancellationToken) {
            return Task.FromResult<IEnumerable<DataQueryFunction>>(DataQueryFunctions);
        }


        /// <summary>
        /// Converts a <see cref="DateTime"/> into an ISO date string with the same precision used by 
        /// Elasticsearch.
        /// </summary>
        /// <param name="time">The time stamp to convert.</param>
        /// <returns>
        /// The converted time stamp.
        /// </returns>
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
        /// An array of tag groups, where each group is an array of tuples, and each tuple contains a 
        /// tag definition and the key to use in the query results dictionary.
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


        /// <summary>
        /// Adds a query for raw data to an Elasticsearch search descriptor.
        /// </summary>
        /// <param name="searchDescriptor">The search descriptor to add the query to.</param>
        /// <param name="tagId">The ID of the tag to query.</param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="pointCount">The maximum number of raw samples to return.</param>
        /// <returns>
        /// The updated <paramref name="searchDescriptor"/>.
        /// </returns>
        private SearchDescriptor<TagValueDocument> AddRawDataQuerySearchDescriptor(SearchDescriptor<TagValueDocument> searchDescriptor, Guid tagId, string utcStartTime, string utcEndTime, int pointCount) {
            return searchDescriptor.Index(_dataQueryIndices)
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


        /// <summary>
        /// Adds a query to an Elasticsearch search descriptor that will retrieve the raw sample for 
        /// a tag prior to the specified time stamp.
        /// </summary>
        /// <param name="searchDescriptor">The search descriptor to add the query to.</param>
        /// <param name="tagId">The ID of the tag to query.</param>
        /// <param name="utcSampleTime">The UTC sample time to retrieve the previous sample for.</param>
        /// <returns>
        /// The updated <paramref name="searchDescriptor"/>.
        /// </returns>
        private SearchDescriptor<TagValueDocument> AddPreviousSampleSearchDescriptor(SearchDescriptor<TagValueDocument> searchDescriptor, Guid tagId, string utcSampleTime) {
            return searchDescriptor.Index(_dataQueryIndices)
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


        /// <summary>
        /// Adds a query to an Elasticsearch search descriptor that will retrieve the raw sample for 
        /// a tag after the specified time stamp.
        /// </summary>
        /// <param name="searchDescriptor">The search descriptor to add the query to.</param>
        /// <param name="tagId">The ID of the tag to query.</param>
        /// <param name="utcSampleTime">The UTC sample time to retrieve the next sample for.</param>
        /// <returns>
        /// The updated <paramref name="searchDescriptor"/>.
        /// </returns>
        private SearchDescriptor<TagValueDocument> AddNextSampleSearchDescriptor(SearchDescriptor<TagValueDocument> searchDescriptor, Guid tagId, string utcSampleTime) {
            return searchDescriptor.Index(_dataQueryIndices)
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


        /// <summary>
        /// Adds a query to an Elasticsearch search descriptor that will select the raw values to use in an aggregated data query.
        /// </summary>
        /// <param name="searchDescriptor">The search descriptor to add the query to.</param>
        /// <param name="tagId">The ID of the tag to query.</param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <returns>
        /// The updated <paramref name="searchDescriptor"/>.
        /// </returns>
        private SearchDescriptor<TagValueDocument> AddAggregatedDataQuerySearchDescriptor(SearchDescriptor<TagValueDocument> searchDescriptor, Guid tagId, string utcStartTime, string utcEndTime) {
            return searchDescriptor.Index(_dataQueryIndices)
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


        /// <summary>
        /// Performs a raw data query.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to query.</param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="pointCount">
        ///   The maximum number of raw samples to return.  A value of less than one indicates that all 
        ///   raw samples in the time range should be returned (up to the limit imposed by the historian).
        /// </param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The raw data samples, indexed by tag.
        /// </returns>
        protected override async Task<IDictionary<TagDefinition, TagValueCollection>> ReadRawData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken) {
            var result = new ConcurrentDictionary<TagDefinition, TagValueCollection>();

            var gte = GetIsoDateString(utcStartTime);
            var lte = GetIsoDateString(utcEndTime);

            var take = pointCount < 1 || pointCount > MaxSamplesPerTagPerQuery
                ? MaxSamplesPerTagPerQuery
                : pointCount;

            var tagGroups = GetTagQueryGroups(tags.OfType<ElasticsearchTagDefinition>(), take);

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


        /// <summary>
        /// Performs a raw data sub-query on behalf of <see cref="ReadRawData(ClaimsPrincipal, IEnumerable{TagDefinition}, DateTime, DateTime, int, CancellationToken)"/>.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to query.</param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="pointCount">
        ///   The maximum number of raw samples to return.  A value of less than one indicates that all 
        ///   raw samples in the time range should be returned (up to the limit imposed by the historian).
        /// </param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The raw data samples, indexed by tag.
        /// </returns>
        private async Task<IDictionary<TagDefinition, TagValueCollection>> ReadRawDataInternal(IEnumerable<ElasticsearchTagDefinition> tags, string utcStartTime, string utcEndTime, int pointCount, CancellationToken cancellationToken) {
            var result = new ConcurrentDictionary<TagDefinition, TagValueCollection>();

            var multiSearchRequest = new MultiSearchDescriptor();
            foreach (var item in tags) {
                multiSearchRequest = multiSearchRequest
                    .Search<TagValueDocument>(selector => AddRawDataQuerySearchDescriptor(selector, item.IdAsGuid, utcStartTime, utcEndTime, pointCount));
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

                result[tag] = new TagValueCollection() {
                    VisualizationHint = TagValueCollectionVisualizationHint.TrailingEdge,
                    Values = item.Documents.Select(x => x.ToTagValue(units: tag.Units)).ToArray()
                };
            }

            return result;
        }


        /// <summary>
        /// Performs a processed (aggregated) data query.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to query.</param>
        /// <param name="dataFunction">The data query function.</param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="pointCount">
        ///   The number of samples to return.  This will be used to calculate the bucket size for 
        ///   data aggregation.
        /// </param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The aggregated data samples, indexed by tag.
        /// </returns>
        protected override Task<IDictionary<TagDefinition, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken) {
            if (pointCount < 1) {
                pointCount = 1;
            }
            var sampleInterval = TimeSpan.FromMilliseconds((utcEndTime - utcStartTime).TotalMilliseconds / pointCount);
            return ReadProcessedData(identity, tags, dataFunction, utcStartTime, utcEndTime, sampleInterval, cancellationToken);
        }


        /// <summary>
        /// Performs a processed (aggregated) data sub-query on behalf of <see cref="ReadProcessedData(ClaimsPrincipal, IEnumerable{TagDefinition}, string, DateTime, DateTime, int, CancellationToken)"/>.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to query.</param>
        /// <param name="dataFunction">The data query function.</param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="pointCount">
        ///   The number of samples to return.  This will be used to calculate the bucket size for 
        ///   data aggregation.
        /// </param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The aggregated data samples, indexed by tag.
        /// </returns>
        protected override async Task<IDictionary<TagDefinition, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, CancellationToken cancellationToken) {
            var result = new ConcurrentDictionary<TagDefinition, TagValueCollection>();

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

            var tagGroups = GetTagQueryGroups(tags.OfType<ElasticsearchTagDefinition>(), pointCount);

            IEnumerable<Task<IDictionary<TagDefinition, TagValueCollection>>> queries = null;

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


        /// <summary>
        /// Performs an average aggregated data query.
        /// </summary>
        /// <param name="tags">The tags to query.</param>
        /// <param name="utcStartTimeAsString">
        ///   The UTC start time for the query, converted to the ISO format used by Elasticsearch.
        /// </param>
        /// <param name="utcStartTimeAsDate">The UTC start time for the query.</param>
        /// <param name="utcEndTimeAsString">
        ///   The UTC end time for the query, converted to the ISO format used by Elasticsearch.
        /// </param>
        /// <param name="utcEndTimeAsDate">The UTC end time for the query.</param>
        /// <param name="sampleInterval">The bucket size for the aggregation.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The aggregated data, indexed by tag.
        /// </returns>
        private async Task<IDictionary<TagDefinition, TagValueCollection>> ReadAverageDataInternal(IEnumerable<ElasticsearchTagDefinition> tags, string utcStartTimeAsString, DateTime utcStartTimeAsDate, string utcEndTimeAsString, DateTime utcEndTimeAsDate, TimeSpan sampleInterval, CancellationToken cancellationToken) {
            var result = new ConcurrentDictionary<TagDefinition, TagValueCollection>();

            var multiSearchRequest = new MultiSearchDescriptor();
            foreach (var item in tags) {
                multiSearchRequest = multiSearchRequest
                    .Search<TagValueDocument>(
                        selector => AddAggregatedDataQuerySearchDescriptor(selector, item.IdAsGuid, utcStartTimeAsString, utcEndTimeAsString)
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

                result[tag] = new TagValueCollection() {
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


        /// <summary>
        /// Performs a minimum aggregated data query.
        /// </summary>
        /// <param name="tags">The tags to query.</param>
        /// <param name="utcStartTimeAsString">
        ///   The UTC start time for the query, converted to the ISO format used by Elasticsearch.
        /// </param>
        /// <param name="utcStartTimeAsDate">The UTC start time for the query.</param>
        /// <param name="utcEndTimeAsString">
        ///   The UTC end time for the query, converted to the ISO format used by Elasticsearch.
        /// </param>
        /// <param name="utcEndTimeAsDate">The UTC end time for the query.</param>
        /// <param name="sampleInterval">The bucket size for the aggregation.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The aggregated data, indexed by tag.
        /// </returns>
        private async Task<IDictionary<TagDefinition, TagValueCollection>> ReadMinimumDataInternal(IEnumerable<ElasticsearchTagDefinition> tags, string utcStartTimeAsString, DateTime utcStartTimeAsDate, string utcEndTimeAsString, DateTime utcEndTimeAsDate, TimeSpan sampleInterval, CancellationToken cancellationToken) {
            var result = new ConcurrentDictionary<TagDefinition, TagValueCollection>();

            var multiSearchRequest = new MultiSearchDescriptor();
            foreach (var item in tags) {
                multiSearchRequest = multiSearchRequest
                    .Search<TagValueDocument>(
                        selector => AddAggregatedDataQuerySearchDescriptor(selector, item.IdAsGuid, utcStartTimeAsString, utcEndTimeAsString)
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

                result[tag] = new TagValueCollection() {
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


        /// <summary>
        /// Performs a maximum aggregated data query.
        /// </summary>
        /// <param name="tags">The tags to query.</param>
        /// <param name="utcStartTimeAsString">
        ///   The UTC start time for the query, converted to the ISO format used by Elasticsearch.
        /// </param>
        /// <param name="utcStartTimeAsDate">The UTC start time for the query.</param>
        /// <param name="utcEndTimeAsString">
        ///   The UTC end time for the query, converted to the ISO format used by Elasticsearch.
        /// </param>
        /// <param name="utcEndTimeAsDate">The UTC end time for the query.</param>
        /// <param name="sampleInterval">The bucket size for the aggregation.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The aggregated data, indexed by tag.
        /// </returns>
        private async Task<IDictionary<TagDefinition, TagValueCollection>> ReadMaximumDataInternal(IEnumerable<ElasticsearchTagDefinition> tags, string utcStartTimeAsString, DateTime utcStartTimeAsDate, string utcEndTimeAsString, DateTime utcEndTimeAsDate, TimeSpan sampleInterval, CancellationToken cancellationToken) {
            var result = new ConcurrentDictionary<TagDefinition, TagValueCollection>();

            var multiSearchRequest = new MultiSearchDescriptor();
            foreach (var item in tags) {
                multiSearchRequest = multiSearchRequest
                    .Search<TagValueDocument>(
                        selector => AddAggregatedDataQuerySearchDescriptor(selector, item.IdAsGuid, utcStartTimeAsString, utcEndTimeAsString)
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

                result[tag] = new TagValueCollection() {
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


        /// <summary>
        /// Performs an interpolated data query.
        /// </summary>
        /// <param name="tags">The tags to query.</param>
        /// <param name="utcStartTimeAsString">
        ///   The UTC start time for the query, converted to the ISO format used by Elasticsearch.
        /// </param>
        /// <param name="utcStartTimeAsDate">The UTC start time for the query.</param>
        /// <param name="utcEndTimeAsString">
        ///   The UTC end time for the query, converted to the ISO format used by Elasticsearch.
        /// </param>
        /// <param name="utcEndTimeAsDate">The UTC end time for the query.</param>
        /// <param name="sampleInterval">The bucket size for the aggregation.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The interpolated data, indexed by tag.
        /// </returns>
        private async Task<IDictionary<TagDefinition, TagValueCollection>> ReadInterpolatedDataInternal(IEnumerable<ElasticsearchTagDefinition> tags, string utcStartTimeAsString, DateTime utcStartTimeAsDate, string utcEndTimeAsString, DateTime utcEndTimeAsDate, TimeSpan bucketSize, CancellationToken cancellationToken) {
            var result = new ConcurrentDictionary<TagDefinition, TagValueCollection>();

            var multiSearchRequest = new MultiSearchDescriptor();
            foreach (var item in tags) {
                multiSearchRequest = multiSearchRequest
                    .Search<TagValueDocument>(
                        selector => AddAggregatedDataQuerySearchDescriptor(selector, item.IdAsGuid, utcStartTimeAsString, utcEndTimeAsString)
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
                    .Search<TagValueDocument>(selector => AddPreviousSampleSearchDescriptor(selector, item.IdAsGuid, utcStartTimeAsString))
                    .Search<TagValueDocument>(selector => AddNextSampleSearchDescriptor(selector, item.IdAsGuid, utcStartTimeAsString));
            }

            var response = await Client.MultiSearchAsync(multiSearchRequest, cancellationToken).ConfigureAwait(false);

            var responsesEnumerator = response.AllResponses.GetEnumerator();
            foreach (var item in tags) {
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
                        resultValues.Add(value0.ToTagValue(units: item.Units));
                        continue;
                    }
                    if (value1 != null && value1.UtcSampleTime == sampleTime) {
                        resultValues.Add(value1.ToTagValue(units: item.Units));
                        continue;
                    }
                    if (value0 != null && value1 != null && sampleTime > value0.UtcSampleTime && sampleTime < value1.UtcSampleTime) {
                        if (Double.IsNaN(value0.NumericValue) || Double.IsNaN(value1.NumericValue)) {
                            resultValues.Add(new TagValue(sampleTime, Double.NaN, null, new[] { value0.Quality, value1.Quality }.Min(), item.Units));
                        }
                        else {
                            resultValues.Add(new TagValue(sampleTime, InterpolateValue(value0.UtcSampleTime.Ticks, value0.NumericValue, value1.UtcSampleTime.Ticks, value1.NumericValue, sampleTime.Ticks), null, new[] { value0.Quality, value1.Quality }.Min(), item.Units));
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
                            resultValues.Add(value0.ToTagValue(units: item.Units));
                            continue;
                        }
                        if (value1 != null && value1.UtcSampleTime == sampleTime) {
                            resultValues.Add(value1.ToTagValue(units: item.Units));
                            continue;
                        }

                        if (Double.IsNaN(value0.NumericValue) || Double.IsNaN(value1.NumericValue)) {
                            resultValues.Add(new TagValue(sampleTime, Double.NaN, null, new[] { value0.Quality, value1.Quality }.Min(), item.Units));
                        }
                        else {
                            resultValues.Add(new TagValue(sampleTime, InterpolateValue(value0.UtcSampleTime.Ticks, value0.NumericValue, value1.UtcSampleTime.Ticks, value1.NumericValue, sampleTime.Ticks), null, new[] { value0.Quality, value1.Quality }.Min(), item.Units));
                        }
                    }
                }

                result[item] = new TagValueCollection() {
                    VisualizationHint = TagValueCollectionVisualizationHint.Interpolated,
                    Values = resultValues
                };
            }

            return result;
        }


        /// <summary>
        /// Performs a visualization-friendly (plot) data query.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to query.</param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="intervals">The number of intervals to use in the query.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The query results, indexed by tag.
        /// </returns>
        /// <remarks>
        /// 
        /// <para>
        /// A plot query divides the query time range into a number of equally-sized <paramref name="intervals"/>.  
        /// In each interval, the historian returns up to 5 samples: the earliest value, the latest 
        /// value, the minimum value, the maximum value, and the earliest value in the interval with 
        /// non-good quality.  It is possible that the same sample meets more than one of the 5 
        /// criteria.
        /// </para>
        /// 
        /// <para>
        /// Plot queries as described above are only possible on numeric tags; for non-numeric tags, a
        /// raw data query is performed instead, with <paramref name="intervals"/> multiplied by 4 used 
        /// as the maximum number of samples to return.
        /// </para>
        /// 
        /// </remarks>
        protected override async Task<IDictionary<TagDefinition, TagValueCollection>> ReadPlotData(ClaimsPrincipal identity, IEnumerable<TagDefinition> tags, DateTime utcStartTime, DateTime utcEndTime, int intervals, CancellationToken cancellationToken) {
            var result = new ConcurrentDictionary<TagDefinition, TagValueCollection>();

            var numericTags = tags.Where(x => x.DataType == TagDataType.FloatingPoint || x.DataType == TagDataType.Integer).ToArray();
            var nonNumericTags = tags.Except(numericTags).ToArray();

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

            var tagGroups = GetTagQueryGroups(numericTags.OfType<ElasticsearchTagDefinition>(), take);

            var numericQueries = tagGroups.Select(grp => ReadPlotDataInternal(grp, gte, utcStartTime, lte, utcEndTime, bucketSize, estimatedPointCount, cancellationToken).ContinueWith(t => {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var item in t.Result) {
                    result[item.Key] = item.Value;
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion)).ToArray();

            var nonNumericQuery = nonNumericTags.Length > 0
                ? ReadRawData(identity, nonNumericTags, utcStartTime, utcEndTime, intervals * 4, cancellationToken).ContinueWith(t => {
                    cancellationToken.ThrowIfCancellationRequested();
                    foreach (var item in t.Result) {
                        result[item.Key] = item.Value;
                    }
                })
            : Task.CompletedTask;

            await Task.WhenAny(Task.WhenAll(numericQueries.Concat(new[] { nonNumericQuery })), Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            return result;
        }


        /// <summary>
        /// Performs a visualization-friendly (plot) data query.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tags">The tags to query.</param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="intervals">The number of intervals to use in the query.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The query results, indexed by tag.
        /// </returns>
        private async Task<IDictionary<TagDefinition, TagValueCollection>> ReadPlotDataInternal(IEnumerable<ElasticsearchTagDefinition> tags, string utcStartTimeAsString, DateTime utcStartTimeAsDate, string utcEndTimeAsString, DateTime utcEndTimeAsDate, TimeSpan bucketSize, int nonNumericPointCount, CancellationToken cancellationToken) {
            var result = new ConcurrentDictionary<TagDefinition, TagValueCollection>();

            var multiSearchRequest = new MultiSearchDescriptor();
            foreach (var item in tags) {
                if (item.DataType == TagDataType.FloatingPoint || item.DataType == TagDataType.Integer) {
                    multiSearchRequest = multiSearchRequest
                        .Search<TagValueDocument>(
                            selector => AddAggregatedDataQuerySearchDescriptor(selector, item.IdAsGuid, utcStartTimeAsString, utcEndTimeAsString)
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
                        .Search<TagValueDocument>(selector => AddPreviousSampleSearchDescriptor(selector, item.IdAsGuid, utcStartTimeAsString))
                        .Search<TagValueDocument>(selector => AddNextSampleSearchDescriptor(selector, item.IdAsGuid, utcStartTimeAsString));
                }
                else {
                    multiSearchRequest = multiSearchRequest
                        .Search<TagValueDocument>(selector => AddRawDataQuerySearchDescriptor(selector, item.IdAsGuid, utcStartTimeAsString, utcEndTimeAsString, nonNumericPointCount))
                        .Search<TagValueDocument>(selector => AddPreviousSampleSearchDescriptor(selector, item.IdAsGuid, utcStartTimeAsString))
                        .Search<TagValueDocument>(selector => AddNextSampleSearchDescriptor(selector, item.IdAsGuid, utcStartTimeAsString));
                }
            }

            var response = await Client.MultiSearchAsync(multiSearchRequest, cancellationToken).ConfigureAwait(false);

            var responsesEnumerator = response.AllResponses.GetEnumerator();
            foreach (var item in tags) {
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

                if (item.DataType == TagDataType.FloatingPoint || item.DataType == TagDataType.Integer) {
                    var values = new List<TagValue>();

                    foreach (var bucket in queryResponse.Aggs.DateHistogram("aika").Buckets) {
                        var bucketVals = new List<TagValue>();

                        foreach (var hit in bucket.TopHits("min_value").Hits<TagValueDocument>()) {
                            bucketVals.Add(hit.Source.ToTagValue(units: item.Units));
                        }
                        foreach (var hit in bucket.TopHits("max_value").Hits<TagValueDocument>()) {
                            bucketVals.Add(hit.Source.ToTagValue(units: item.Units));
                        }
                        foreach (var hit in bucket.TopHits("earliest_value").Hits<TagValueDocument>()) {
                            bucketVals.Add(hit.Source.ToTagValue(units: item.Units));
                        }
                        foreach (var hit in bucket.TopHits("latest_value").Hits<TagValueDocument>()) {
                            bucketVals.Add(hit.Source.ToTagValue(units: item.Units));
                        }
                        foreach (var hit in bucket.TopHits("quality_value").Hits<TagValueDocument>()) {
                            if (hit.Source.Quality == TagValueQuality.Good) {
                                continue;
                            }
                            bucketVals.Add(hit.Source.ToTagValue(units: item.Units));
                        }

                        values.AddRange(bucketVals.Distinct().OrderBy(x => x.UtcSampleTime));
                    }

                    var first = values.FirstOrDefault();
                    if (first != null && first.UtcSampleTime > utcStartTimeAsDate) {
                        var prev = previousValueResponse.Documents.FirstOrDefault();
                        if (prev != null && !Double.IsNaN(prev.NumericValue)) {
                            values.Insert(0, new TagValue(utcStartTimeAsDate, InterpolateValue(prev.UtcSampleTime.Ticks, prev.NumericValue, first.UtcSampleTime.Ticks, first.NumericValue, utcStartTimeAsDate.Ticks), null, prev.Quality, item.Units));
                        }
                    }

                    var last = values.LastOrDefault();
                    if (last != null && last.UtcSampleTime < utcEndTimeAsDate) {
                        var next = nextValueResponse.Documents.FirstOrDefault();
                        if (next != null && !Double.IsNaN(next.NumericValue)) {
                            values.Add(new TagValue(utcEndTimeAsDate, InterpolateValue(last.UtcSampleTime.Ticks, last.NumericValue, next.UtcSampleTime.Ticks, next.NumericValue, utcEndTimeAsDate.Ticks), null, next.Quality, item.Units));
                        }
                    }

                    result[item] = new TagValueCollection() {
                        VisualizationHint = TagValueCollectionVisualizationHint.Interpolated,
                        Values = values
                    };
                }
                else {
                    var values = queryResponse.Documents.Select(x => x.ToTagValue(units: item.Units)).ToList();

                    var first = values.FirstOrDefault();
                    if (first != null && first.UtcSampleTime > utcStartTimeAsDate) {
                        var prev = previousValueResponse.Documents.FirstOrDefault();
                        if (prev != null) {
                            values.Insert(0, prev.ToTagValue(utcSampleTime: utcStartTimeAsDate, units: item.Units));
                        }
                    }

                    var last = values.LastOrDefault();
                    if (last != null && last.UtcSampleTime < utcEndTimeAsDate) {
                        var next = nextValueResponse.Documents.FirstOrDefault();
                        if (next != null) {
                            values.Add(next.ToTagValue(utcSampleTime: utcEndTimeAsDate, units: item.Units));
                        }
                    }

                    result[item] = new TagValueCollection() {
                        VisualizationHint = TagValueCollectionVisualizationHint.TrailingEdge,
                        Values = values
                    };
                }
            }

            return result;
        }


        /// <summary>
        /// Interpolates a value on a slope.
        /// </summary>
        /// <param name="x0">The time stamp (in ticks) of the earlier point on the slope.</param>
        /// <param name="y0">The value of the earlier point on the slope</param>
        /// <param name="x1">The time stamp (in ticks) of the later point on the slope.</param>
        /// <param name="y1">The value of the later point on the slope.</param>
        /// <param name="x">The time stamp (in ticks) to interpolate the Y-value at.</param>
        /// <returns>
        /// The interpolated value.
        /// </returns>
        private static double InterpolateValue(double x0, double y0, double x1, double y1, double x) {
            return ((y0 * (x1 - x)) + (y1 * (x - x0))) / (x1 - x0);
        }

        #endregion

        #region [ Write Tag Data ]

        /// <summary>
        /// Sends an updated snapshot value to the snapshot writer.
        /// </summary>
        /// <param name="tag">The tag for the value.</param>
        /// <param name="value">The new value.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A completed task; the write is performed in the background.
        /// </returns>
        internal Task WriteSnapshotValue(ElasticsearchTagDefinition tag, TagValue value, CancellationToken cancellationToken) {
            _snapshotWriter.WriteValue(tag.IdAsGuid, value);
            return Task.CompletedTask;
        }


        /// <summary>
        /// Inserts values into the Elasticsearch archive indices.
        /// </summary>
        /// <param name="tag">The tag for the values.</param>
        /// <param name="values">The values to insert.</param>
        /// <param name="nextArchiveCandidate">
        ///   The updated archive candidate for the tag (i.e. the next value that will potentially be 
        ///   written to the permanent archive).  This can be <see langword="null"/> e.g. if a bulk
        ///   insert of history is being performed.
        /// </param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The write result.  Note that, since writing to the archive is performed in the background, 
        /// the result does not reflect the final written states of the values.
        /// </returns>
        internal async Task<WriteTagValuesResult> InsertArchiveValues(ElasticsearchTagDefinition tag, IEnumerable<TagValue> values, ArchiveCandidateValue nextArchiveCandidate, CancellationToken cancellationToken) {
            foreach (var value in (values ?? new TagValue[0]).Concat(new[] { nextArchiveCandidate?.Value })) {
                if (value == null) {
                    continue;
                }

                await CreateArchiveIndex(tag, value.UtcSampleTime, cancellationToken).ConfigureAwait(false);
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
