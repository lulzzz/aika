using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aika.Client.Dto {
    /// <summary>
    /// Provides information about the Aika historian.
    /// </summary>
    public class AikaInfoDto {

        /// <summary>
        /// Gets or sets the version number.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the UTC startup time for the Aika historian.
        /// </summary>
        public DateTime UtcStartupTime { get; set; }

        /// <summary>
        /// Gets or sets system properties.
        /// </summary>
        public IDictionary<string, string> Properties { get; set; }

    }
}
