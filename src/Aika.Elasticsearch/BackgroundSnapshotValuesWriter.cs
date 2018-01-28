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
    /// Writes snapshot values to Elasticsearch in periodic background tasks.
    /// </summary>
    internal class BackgroundSnapshotValuesWriter : IDisposable {

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
        /// The snapshot values that will be written by the next write operation, indexed by tag name.
        /// </summary>
        private readonly ConcurrentDictionary<Guid, TagValue> _values = new ConcurrentDictionary<Guid, TagValue>();

        /// <summary>
        /// Locks access to <see cref="_values"/> when a write to Elasticsearch is going to be performed.
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
        /// Creates a new <see cref="BackgroundSnapshotValuesWriter"/> object.
        /// </summary>
        /// <param name="historian">The owning historian.</param>
        /// <param name="interval">The interval between writes.</param>
        /// <param name="loggerFactory">The logger factory to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="historian"/> is <see langword="null"/>.</exception>
        internal BackgroundSnapshotValuesWriter(ElasticsearchHistorian historian, TimeSpan writeInterval, ILoggerFactory loggerFactory) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
            _logger = loggerFactory?.CreateLogger<BackgroundSnapshotValuesWriter>();
            _interval = writeInterval;
        }


        /// <summary>
        /// Enqueues the specified snapshot value for writing.
        /// </summary>
        /// <param name="tagId">The tag ID.</param>
        /// <param name="value">The tag value.</param>
        internal void WriteValue(Guid tagId, TagValue value) {
            _valuesLock.EnterReadLock();
            try {
                _values[tagId] = value;
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

                IDictionary<Guid, TagValue> values;

                _valuesLock.EnterWriteLock();
                try {
                    if (_values.Count == 0) {
                        _taskLock = 0;
                        continue;
                    }
                    values = _values.ToDictionary(x => x.Key, x => x.Value);
                    _values.Clear();
                }
                finally {
                    _valuesLock.ExitWriteLock();
                }

                _historian.TaskRunner.RunBackgroundTask(async ct => {
                    try {
                        var descriptor = new BulkDescriptor();
                        var send = false;

                        foreach (var item in values) {
                            var tag = _historian.GetTagById(item.Key);
                            if (tag == null) {
                                continue;
                            }
                            var op = new BulkIndexOperation<TagValueDocument>(item.Value.ToTagValueDocument(tag, tag.IdAsGuid)) {
                                Index = _historian.SnapshotValuesIndexName
                            };
                            descriptor.AddOperation(op);
                            send = true;
                        }

                        if (send) {
                            await _historian.Client.BulkAsync(descriptor, ct).ConfigureAwait(false);
                        }
                    }
                    catch (Exception e) {
                        _logger?.LogError("An error occurred while writing a bulk snapshot update.", e);
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
