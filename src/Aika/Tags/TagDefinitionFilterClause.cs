using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Tags {

    /// <summary>
    /// Describes a clause in a <see cref="TagDefinitionFilter"/>.
    /// </summary>
    public class TagDefinitionFilterClause {

        /// <summary>
        /// Gets or sets the field that the filter applies to.
        /// </summary>
        public TagDefinitionFilterField Field { get; set; }

        /// <summary>
        /// Gets or sets the filter value.
        /// </summary>
        public string Value { get; set; }

    }


    /// <summary>
    /// Describes the tag definition field that the filter applies to.
    /// </summary>
    public enum TagDefinitionFilterField {

        /// <summary>
        /// The filter is for the tag name.
        /// </summary>
        Name,

        /// <summary>
        /// The filter is for the tag description.
        /// </summary>
        Description,

        /// <summary>
        /// The filter is for the tag units.
        /// </summary>
        Units

    }
}
