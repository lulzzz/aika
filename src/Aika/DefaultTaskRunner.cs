using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aika {

    /// <summary>
    /// Default <see cref="ITaskRunner"/> implementation that uses <see cref="Task.Run(Func{Task})"/> 
    /// to run tasks in the background.
    /// </summary>
    public sealed class DefaultTaskRunner : ITaskRunner, IDisposable {

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The cancellation token source for the task runner itself.
        /// </summary>
        private readonly CancellationTokenSource _ctSource = new CancellationTokenSource();


        /// <summary>
        /// Creates a new <see cref="DefaultTaskRunner"/> object.
        /// </summary>
        /// <param name="logger">The logger for the instance.</param>
        public DefaultTaskRunner(ILogger<DefaultTaskRunner> logger) {
            _logger = logger;
        }


        /// <summary>
        /// Runs a background task.
        /// </summary>
        /// <param name="action">The action to run in the background.</param>
        /// <param name="cancellationTokens">Additional cancellation tokens to observe while running the action.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        public void RunBackgroundTask(Func<CancellationToken, Task> action, params CancellationToken[] cancellationTokens) {
            if (action == null) {
                throw new ArgumentNullException(nameof(action));
            }

            if (_ctSource.IsCancellationRequested) {
                return;
            }

            Task.Run(async () => {
                using (var combinedCtSource = CancellationTokenSource.CreateLinkedTokenSource(new[] { _ctSource.Token }.Concat(cancellationTokens).ToArray())) {
                    try {
                        await Task.WhenAny(action(combinedCtSource.Token), Task.Delay(-1, combinedCtSource.Token)).ConfigureAwait(false);
                        combinedCtSource.Token.ThrowIfCancellationRequested();
                    }
                    catch (OperationCanceledException) {
                        // Do nothing.
                    }
                    catch (Exception e) {
                        _logger?.LogError("An error occurred while running a background task.", e);
                    }
                }
            });
        }


        /// <summary>
        /// Disposes of the <see cref="DefaultTaskRunner"/>.
        /// </summary>
        public void Dispose() {
            _ctSource.Dispose();
        }

    }
}
