using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aika.AspNetCore {
    /// <summary>
    /// Options for an <see cref="AikaService"/>.
    /// </summary>
    public class AikaServiceOptions {

        /// <summary>
        /// Gets or sets a delegate to call immediately before intializing the <see cref="AikaService"/>.
        /// </summary>
        public Func<CancellationToken, Task> OnBeforeInit { get; set; }

        /// <summary>
        /// Gets or sets a delegate to call immediately after initializing the <see cref="AikaService"/>.
        /// </summary>
        public Func<CancellationToken, Task> OnAfterInit { get; set; }

    }
}
