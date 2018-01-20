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
    internal class BackgroundSnapshotValuesWriter : IDisposable {

        private readonly ILogger _logger;

        private readonly ElasticsearchHistorian _historian;

        private readonly TimeSpan _interval;

        private readonly ConcurrentDictionary<Guid, TagValue> _values = new ConcurrentDictionary<Guid, TagValue>();

        private readonly ReaderWriterLockSlim _valuesLock = new ReaderWriterLockSlim();

        private int _taskLock;

        private readonly CancellationTokenSource _ctSource = new CancellationTokenSource();


        internal BackgroundSnapshotValuesWriter(ElasticsearchHistorian historian, TimeSpan writeInterval, ILoggerFactory loggerFactory) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
            _logger = loggerFactory?.CreateLogger<BackgroundSnapshotValuesWriter>();
            _interval = writeInterval;
        }


        internal void WriteValue(Guid tagId, TagValue value) {
            _valuesLock.EnterReadLock();
            try {
                _values[tagId] = value;
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
                                Index = ElasticsearchHistorian.SnapshotValuesIndexName
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


        public void Dispose() {
            _ctSource.Dispose();
        }
    }
}
