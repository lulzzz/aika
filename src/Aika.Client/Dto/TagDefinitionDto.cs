using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aika;

namespace Aika.Client.Dto {
    /// <summary>
    /// Describes a tag in the Aika historian.
    /// </summary>
    public class TagDefinitionDto {

        /// <summary>
        /// Gets or sets the unique identifier for the tag.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the tag.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the tag description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the tag units.
        /// </summary>
        public string Units { get; set; }

        /// <summary>
        /// Gets or sets the data type of the tag.
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Gets or sets the name of the discrete state set for the tag, when <see cref="DataType"/> 
        /// is <see cref="TagDataType.State"/>.
        /// </summary>
        public string StateSet { get; set; }

    }
}
