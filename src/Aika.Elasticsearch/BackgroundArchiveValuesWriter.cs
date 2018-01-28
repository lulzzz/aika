using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika.Elasticsearch.Documents;
using Aika.Tags;
using Microsoft.Extensions.Logging;
using Nest;

namespace Aika.Elasticsearch {

    /// <summary>
    /// Writes archive values to Elasticsearch in periodic background tasks.
    /// </summary>
    internal class BackgroundArchiveValuesWriter : IDisposable {

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The historian that owns the writer.
        /// </summary>
        private readonly ElasticsearchHistorian _historian;

        /// <summary>
        /// The interval between writes.
        /// </summary>
        private readonly TimeSpan _interval;

        /// <summary>
        /// A NEST bulk descriptor that represents the next write operation.
        /// </summary>
        private BulkDescriptor _nextInsert;

        /// <summary>
        /// Flags if there are any pending writes to perform.
        /// </summary>
        private bool _writeOperationsPending;

        /// <summary>
        /// The current state of the archive candidate values indexed by tag ID.
        /// </summary>
        private readonly ConcurrentDictionary<Guid, TagValue> _archiveCandidateValues = new ConcurrentDictionary<Guid, TagValue>();

        /// <summary>
        /// Locks access to <see cref="_nextInsert"/> and <see cref="_archiveCandidateValues"/> when a 
        /// write to Elasticsearch is going to be performed.
        /// </summary>
        private readonly ReaderWriterLockSlim _valuesLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Used to ensure that only one write operation can run at a time.
        /// </summary>
        private int _taskLock;

        /// <summary>
        /// Cancellation token source that fires when the object is being disposed.
        /// </summary>
        private readonly CancellationTokenSource _ctSource = new CancellationTokenSource();


        /// <summary>
        /// Creates a new <see cref="BackgroundArchiveValuesWriter"/> object.
        /// </summary>
        /// <param name="historian">The owning historian.</param>
        /// <param name="interval">The interval between writes.</param>
        /// <param name="loggerFactory">The logger factory to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="historian"/> is <see langword="null"/>.</exception>
        internal BackgroundArchiveValuesWriter(ElasticsearchHistorian historian, TimeSpan interval, ILoggerFactory loggerFactory) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
            _logger = loggerFactory?.CreateLogger<BackgroundSnapshotValuesWriter>();
            _interval = interval;
            _nextInsert = new BulkDescriptor();
            _writeOperationsPending = false;
        }
        

        /// <summary>
        /// Enqueues the specified values for writing.
        /// </summary>
        /// <param name="tag">The tag that the values are being written for.</param>
        /// <param name="values">The values to write to the archive.</param>
        /// <param name="archiveCandidate">The current archive candidate value for the tag.</param>
        internal void WriteValues(ElasticsearchTagDefinition tag, IEnumerable<TagValue> values, TagValue archiveCandidate) {
            _valuesLock.EnterReadLock();
            try {
                foreach (var value in values ?? new TagValue[0]) {
                    var op = new BulkIndexOperation<TagValueDocument>(value.ToTagValueDocument(tag, null)) {
                        Index = DocumentMappings.GetIndexNameForArchiveTagValue(_historian.ArchiveIndexNamePrefix, tag, value.UtcSampleTime, _historian.ArchiveIndexSuffixGenerator)
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


        /// <summary>
        /// Runs the writer operation.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token that will signal when the writer is to stop.</param>
        /// <returns>
        /// A task that will complete when cancellation is requested.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        internal async Task Execute(CancellationToken cancellationToken) {
            if (_ctSource.Token.IsCancellationRequested) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            cancellationToken.Register(() => _ctSource.Cancel());
            while (!_ctSource.Token.IsCancellationRequested) {
                await Task.Delay(_interval, _ctSource.Token).ConfigureAwait(false);

                if (Interlocked.CompareExchange(ref _taskLock, 1, 0) != 0) {
                    continue;
                }

                BulkDescriptor descriptor;
                IDictionary<Guid, TagValue> values;
                bool dirty = _writeOperationsPending;

                _valuesLock.EnterWriteLock();
                try {
                    descriptor = _nextInsert;
                    _nextInsert = new BulkDescriptor();
                    _writeOperationsPending = false;

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
                        Index = _historian.ArchiveCandidatesIndexName
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


        /// <summary>
        /// Triggers cancellation of the <see cref="Execute(CancellationToken)"/> method.
        /// </summary>
        public void Dispose() {
            _ctSource.Dispose();
        }
    }
}
