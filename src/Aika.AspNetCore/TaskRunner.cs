using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aika.AspNetCore {
    /// <summary>
    /// <see cref="IHostedService"/> that allows Aika to run background tasks that can be tracked by 
    /// the ASP.NET Core application.
    /// </summary>
    public class TaskRunner : IHostedService, ITaskRunner, IDisposable {

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger _log;

        /// <summary>
        /// A cancellation token source that is used to signal to tasks that the application is shutting down.
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Tracks running tasks.
        /// </summary>
        private readonly ConcurrentDictionary<Task, object> _tasks = new ConcurrentDictionary<Task, object>();


        /// <summary>
        /// Creates a new <see cref="TaskRunner"/> object.
        /// </summary>
        /// <param name="logger">The logger to use.</param>
        public TaskRunner(ILogger<TaskRunner> logger) {
            _log = logger;
        }


        /// <summary>
        /// Starts the hosted service.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to end the startup task.</param>
        /// <returns>
        /// A task that will complete when <paramref name="cancellationToken"/> is cancelled.
        /// </returns>
        public Task StartAsync(CancellationToken cancellationToken) {
            _log?.LogInformation("Startup requested.");
            return Task.Delay(-1, cancellationToken);
        }


        /// <summary>
        /// Stops the hosted service.
        /// </summary>
        /// <param name="cancellationToken">
        ///   A cancellation token that will fire when the graceful shutdown period for running 
        ///   tasks has expired.
        /// </param>
        /// <returns>
        /// A task that will complete when all background tasks have completed, or when 
        /// <paramref name="cancellationToken"/> fires (whichever occurs first).
        /// </returns>
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


        /// <summary>
        /// Registers a task to run in the background.
        /// </summary>
        /// <param name="action">The delegate to run in the background.</param>
        /// <param name="cancellationTokens">Additional cancellation tokens that can be used to cancel the task.</param>
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


        /// <summary>
        /// Disposes of the <see cref="TaskRunner"/> by informing all background tasks to shut down.
        /// </summary>
        public void Dispose() {
            _cancellationTokenSource.Dispose();
        }

    }
}
