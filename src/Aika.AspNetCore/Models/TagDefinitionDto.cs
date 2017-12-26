using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aika;

namespace Aika.AspNetCore.Models {
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
        public TagDataType DataType { get; set; }

        /// <summary>
        /// Gets or sets the name of the discrete state set for the tag, when <see cref="DataType"/> 
        /// is <see cref="TagDataType.State"/>.
        /// </summary>
        public string StateSet { get; set; }


        /// <summary>
        /// Creates a new <see cref="TagDefinitionDto"/> object.
        /// </summary>
        public TagDefinitionDto() { }


        /// <summary>
        /// Creates a new <see cref="TagDefinitionDto"/> object from the specified <see cref="TagDefinition"/>.
        /// </summary>
        /// <param name="tagDefinition">The tag definition to copy from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tagDefinition"/> is <see langword="null"/>.</exception>
        internal TagDefinitionDto(TagDefinition tagDefinition) {
            if (tagDefinition == null) {
                throw new ArgumentNullException(nameof(tagDefinition));
            }

            Id = tagDefinition.Id;
            Name = tagDefinition.Name;
            Description = tagDefinition.Description;
            Units = tagDefinition.Units;
            DataType = tagDefinition.DataType;
            StateSet = tagDefinition.DataType == TagDataType.State 
                ? tagDefinition.StateSet 
                : null;
        }

    }
}
