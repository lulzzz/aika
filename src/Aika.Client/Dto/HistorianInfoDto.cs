using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aika.Client.Dto {
    /// <summary>
    /// Describes the underlying historian implementation being used by the Aika historian.
    /// </summary>
    public class HistorianInfoDto {

        /// <summary>
        /// Gets or sets the type name of the historian implementation.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Gets or sets the version of the historian.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the historian description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets bespoke properties about the historian.
        /// </summary>
        public IDictionary<string, string> Properties { get; set; }

    }
}
