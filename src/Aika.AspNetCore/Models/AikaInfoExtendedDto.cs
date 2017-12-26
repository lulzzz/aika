using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aika.AspNetCore.Models {

    /// <summary>
    /// Describes extended information about the Aika historian.
    /// </summary>
    public class AikaInfoExtendedDto : AikaInfoDto {

        /// <summary>
        /// Gets or sets information about the underlying historian.
        /// </summary>
        public HistorianInfoDto Historian { get; set; }


        /// <summary>
        /// Creates a new <see cref="AikaInfoExtendedDto"/> object.
        /// </summary>
        public AikaInfoExtendedDto() { }


        /// <summary>
        /// Creates a new <see cref="AikaInfoExtendedDto"/> using the specified <see cref="IHistorian"/>.
        /// </summary>
        /// <param name="historian">The <see cref="IHistorian"/> to create the <see cref="Historian"/> information from.</param>
        internal AikaInfoExtendedDto(IHistorian historian) : this() {
            Historian = historian == null
                ? null
                : new HistorianInfoDto(historian);
        }

    }
}
