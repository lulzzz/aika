using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aika.AspNetCore.Models {
    /// <summary>
    /// Descrribes the underlying <see cref="IHistorian"/> implementation being used by the Aika historian.
    /// </summary>
    public class HistorianInfoDto {

        /// <summary>
        /// Gets or sets the type name of the <see cref="IHistorian"/> implementation.
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


        /// <summary>
        /// Creates a new <see cref="HistorianInfoDto"/> object.
        /// </summary>
        public HistorianInfoDto() { }


        /// <summary>
        /// Creates a new <see cref="HistorianInfoDto"/> object from the specified <see cref="IHistorian"/>.
        /// </summary>
        /// <param name="historian">The <see cref="IHistorian"/> to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="historian"/> is <see langword="null"/>.</exception>
        internal HistorianInfoDto(IHistorian historian) {
            if (historian == null) {
                throw new ArgumentNullException(nameof(historian));
            }

            TypeName = historian.GetType().FullName;
            Version = historian.GetType().Assembly?.GetName()?.Version.ToString();
            Description = historian.Description;
            Properties = historian.Properties == null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(historian.Properties);
        }

    }
}
