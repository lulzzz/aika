using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aika {
    /// <summary>
    /// Defines methods for registering background tasks in an application.
    /// </summary>
    public interface ITaskRunner {

        /// <summary>
        /// When implemented in a derived type, registers a task to run in the background.
        /// </summary>
        /// <param name="action">The delegate to run in the background.</param>
        /// <param name="cancellationTokens">Additional cancellation tokens to observe while running the task.</param>
        /// <remarks>
        /// When <see cref="RunBackgroundTask(Func{CancellationToken, Task}, CancellationToken[])"/> is 
        /// invoked, the <see cref="ITaskRunner"/> is expected to pass a <see cref="CancellationToken"/> 
        /// to the provided delegate that is a composite of the following items:
        /// 
        /// 1. A global <see cref="CancellationToken"/> that will fire when the hosting application is 
        ///    shutting down.
        /// 2. Any additional <see cref="CancellationToken"/> instances that were passed to the via the 
        ///    <paramref name="cancellationTokens"/> parameter.
        /// </remarks>
        void RunBackgroundTask(Func<CancellationToken, Task> action, params CancellationToken[] cancellationTokens);

    }
}
