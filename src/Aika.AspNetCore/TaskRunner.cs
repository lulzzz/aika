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
    public class TaskRunner : HostedService, ITaskRunner{

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
        /// Runs the task runner.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token that is used to trigger a shutdown of the task runner.</param>
        /// <returns>
        /// The task runner's service task.
        /// </returns>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
            _log?.LogInformation("Startup requested.");

            cancellationToken.Register(() => {
                _log?.LogInformation("Shutdown requested.");
                _cancellationTokenSource.Cancel();
            });

            try {
                await Task.Delay(-1, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                // No special error handling required - we're shutting down.
            }

            Task[] pendingTasks;

            try {
                _cancellationTokenSource.Cancel();
            }
            finally {
                pendingTasks = _tasks.Keys.ToArray();
            }

            await Task.WhenAll(pendingTasks).ConfigureAwait(false);
        }
    }
}
