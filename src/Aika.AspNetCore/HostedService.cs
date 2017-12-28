using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Aika.AspNetCore {
    /// <summary>
    /// Base <see cref="IHostedService"/> implementation.
    /// </summary>
    /// <remarks>
    /// Adapted from code by Steve Gordon, available at https://github.com/stevejgordon/IHostedServiceSample.
    /// </remarks>
    public abstract class HostedService : IHostedService {
        // Example untested base class code kindly provided by David Fowler: https://gist.github.com/davidfowl/a7dd5064d9dcf35b6eae1a7953d615e3

        /// <summary>
        /// The service task.
        /// </summary>
        private Task _executingTask;

        /// <summary>
        /// The cancellation token source that the service task will observe.
        /// </summary>
        private CancellationTokenSource _cts;


        /// <summary>
        /// Starts the hosted service.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the service.</param>
        /// <returns>
        /// The service task.
        /// </returns>
        public Task StartAsync(CancellationToken cancellationToken) {
            if (_executingTask != null) {
                return _executingTask;
            }

            // Create a linked token so we can trigger cancellation outside of this token's cancellation
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Store the task we're executing
            _executingTask = ExecuteAsync(_cts.Token);

            // If the task is completed then return it, otherwise it's running
            return _executingTask.IsCompleted 
                ? _executingTask 
                : Task.CompletedTask;
        }


        /// <summary>
        /// Stops the hosted service.
        /// </summary>
        /// <param name="cancellationToken">
        ///   A cancellation token that fill trigger when the graceful shutdown period has expired.
        /// </param>
        /// <returns>
        /// A task that will complete when either the service task has completed or the graceful 
        /// shutdown period has expired.
        /// </returns>
        public async Task StopAsync(CancellationToken cancellationToken) {
            // Stop called without start
            if (_executingTask == null) {
                return;
            }

            // Signal cancellation to the executing method
            _cts.Cancel();

            // Wait until the task completes or the stop token triggers
            await Task.WhenAny(_executingTask, Task.Delay(-1, cancellationToken));

            // Throw if cancellation triggered
            cancellationToken.ThrowIfCancellationRequested();
        }


        /// <summary>
        /// When implemented in a derived class, runs a hosted service.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token that the service task should observe.</param>
        /// <returns>
        /// The service task.
        /// </returns>
        protected abstract Task ExecuteAsync(CancellationToken cancellationToken);
    }
}
