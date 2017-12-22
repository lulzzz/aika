using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aika.AspNetCore
{
    public class TaskRunner : IHostedService, ITaskRunner, IDisposable {

        private readonly ILogger _log;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly ConcurrentDictionary<Task, object> _tasks = new ConcurrentDictionary<Task, object>();


        public TaskRunner(ILoggerFactory loggerFactory) {
            _log = loggerFactory?.CreateLogger<TaskRunner>();
        }


        public Task StartAsync(CancellationToken cancellationToken) {
            _log?.LogInformation("Startup requested.");
            return Task.Delay(-1, cancellationToken);
        }


        public async Task StopAsync(CancellationToken cancellationToken) {
            if (_cancellationTokenSource.IsCancellationRequested) {
                return;
            }

            _log?.LogInformation("Shutdown requested.");
            Task[] tasks;

            try {
                _cancellationTokenSource.Cancel();
            }
            finally {
                tasks = _tasks.Keys.ToArray();
            }

            var remainingTasks = Task.WhenAll(tasks);
            await Task.WhenAny(remainingTasks, Task.Delay(-1, cancellationToken)).ConfigureAwait(false);

        }


        public void RunBackgroundTask(Func<CancellationToken, Task> action, params CancellationToken[] cancellationTokens) {
            if (_cancellationTokenSource.IsCancellationRequested) {
                return;
            }

            var task = Task.Run(async () => {
                try {
                    using (var ct = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokens.Concat(new[] { _cancellationTokenSource.Token }).ToArray())) {
                        await action(ct.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) {
                    // Do nothing.
                }
                catch (Exception e) {
                    _log?.LogError($"An unhandled exception occurred in a background task.", e);
                }
            }, _cancellationTokenSource.Token);

            _tasks[task] = new object();

            task.ContinueWith(t => {
                _tasks.TryRemove(t, out var _);
            });
        }


        public void Dispose() {
            _cancellationTokenSource.Dispose();
        }

    }
}
