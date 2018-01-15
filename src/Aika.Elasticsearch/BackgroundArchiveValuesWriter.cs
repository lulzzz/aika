using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika.Elasticsearch.Documents;
using Microsoft.Extensions.Logging;
using Nest;

namespace Aika.Elasticsearch {
    internal class BackgroundArchiveValuesWriter : IDisposable {

        private readonly ILogger _logger;

        private readonly ElasticHistorian _historian;

        private readonly TimeSpan _interval;

        private BulkDescriptor _nextInsert;

        private bool _dirty;

        private readonly ConcurrentDictionary<Guid, TagValue> _archiveCandidateValues = new ConcurrentDictionary<Guid, TagValue>();

        private readonly ReaderWriterLockSlim _valuesLock = new ReaderWriterLockSlim();

        private int _taskLock;

        private readonly CancellationTokenSource _ctSource = new CancellationTokenSource();


        internal BackgroundArchiveValuesWriter(ElasticHistorian historian, TimeSpan interval, ILoggerFactory loggerFactory) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
            _logger = loggerFactory?.CreateLogger<BackgroundSnapshotValuesWriter>();
            _interval = interval;
            _nextInsert = new BulkDescriptor();
            _dirty = false;
        }
        

        internal void WriteValues(ElasticTagDefinition tag, IEnumerable<TagValue> values, TagValue archiveCandidate) {
            _valuesLock.EnterReadLock();
            try {
                foreach (var value in values ?? new TagValue[0]) {
                    var op = new BulkIndexOperation<TagValueDocument>(value.ToTagValueDocument(tag, null)) {
                        Index = DocumentMappings.GetIndexNameForArchiveTagValue(ElasticHistorian.ArchiveIndexNamePrefix, value.UtcSampleTime)
                    };
                    _nextInsert.AddOperation(op);
                }
                if (archiveCandidate != null) {
                    _archiveCandidateValues[tag.IdAsGuid] = archiveCandidate;
                }
            }
            finally {
                _valuesLock.ExitReadLock();
            }
        }


        internal async Task Execute(CancellationToken cancellationToken) {
            cancellationToken.Register(() => _ctSource.Cancel());
            while (!_ctSource.Token.IsCancellationRequested) {
                await Task.Delay(_interval, _ctSource.Token).ConfigureAwait(false);

                if (Interlocked.CompareExchange(ref _taskLock, 1, 0) != 0) {
                    continue;
                }

                BulkDescriptor descriptor;
                IDictionary<Guid, TagValue> values;
                bool dirty = _dirty;

                _valuesLock.EnterWriteLock();
                try {
                    descriptor = _nextInsert;
                    _nextInsert = new BulkDescriptor();
                    _dirty = false;

                    values = _archiveCandidateValues.ToDictionary(x => x.Key, x => x.Value);
                    _archiveCandidateValues.Clear();
                }
                finally {
                    _valuesLock.ExitWriteLock();
                }

                foreach (var item in values) {
                    var tag = _historian.GetTagById(item.Key);
                    if (tag == null) {
                        continue;
                    }

                    var op = new BulkIndexOperation<TagValueDocument>(item.Value.ToTagValueDocument(tag, tag.IdAsGuid)) {
                        Index = ElasticHistorian.ArchiveCandidatesIndexName
                    };
                    descriptor.AddOperation(op);
                    dirty = true;
                }

                if (!dirty) {
                    _taskLock = 0;
                    continue;
                }

                _historian.TaskRunner.RunBackgroundTask(async ct => {
                    try {
                        await _historian.Client.BulkAsync(descriptor, ct).ConfigureAwait(false);
                    }
                    catch (Exception e) {
                        _logger?.LogError("An error occurred while writing a bulk archive update.", e);
                    }
                    finally {
                        _taskLock = 0;
                    }
                }, _ctSource.Token);
            }
        }


        public void Dispose() {
            _ctSource.Dispose();
        }
    }
}
