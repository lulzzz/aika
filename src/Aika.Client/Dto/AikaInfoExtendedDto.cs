using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aika.Client.Dto {

    /// <summary>
    /// Describes extended information about the Aika historian.
    /// </summary>
    public class AikaInfoExtendedDto : AikaInfoDto {

        /// <summary>
        /// Gets or sets information about the underlying historian.
        /// </summary>
        public HistorianInfoDto Historian { get; set; }

    }
}
