using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Aika.AspNetCore.Models.Query;

namespace Aika.AspNetCore.Models.Tags {
    /// <summary>
    /// Describes an update to a tag definition.
    /// </summary>
    public class TagDefinitionUpdateDto {

        /// <summary>
        /// Gets or sets the tag name.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the tag description.
        /// </summary>
        [MaxLength(1000)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the tag units.
        /// </summary>
        [MaxLength(50)]
        public string Units { get; set; }

        /// <summary>
        /// Gets or sets the tag's data type.
        /// </summary>
        [Required]
        public TagDataType DataType { get; set; }

        /// <summary>
        /// Gets or sets the name of the discrete state set to use for the tag, when <see cref="DataType"/> 
        /// is <see cref="TagDataType.State"/>.
        /// </summary>
        public string StateSet { get; set; }

        /// <summary>
        /// Gets or sets the exception filter settings for the tag.
        /// </summary>
        public TagValueFilterSettingsDto ExceptionFilterSettings { get; set; }

        /// <summary>
        /// Gets or sets the compression filter settings for the tag.
        /// </summary>
        public TagValueFilterSettingsDto CompressionFilterSettings { get; set; }


        /// <summary>
        /// Converts the object into a <see cref="TagDefinitionUpdate"/> object.
        /// </summary>
        /// <returns>
        /// An equivalent <see cref="TagDefinitionUpdate"/> object.
        /// </returns>
        internal TagDefinitionUpdate ToTagDefinitionUpdate() {
            return new TagDefinitionUpdate() {
                Name = Name,
                Description = Description,
                Units = Units,
                DataType = DataType,
                StateSet = StateSet,
                ExceptionFilterSettings = ExceptionFilterSettings?.ToTagValueFilterSettingsUpdate(),
                CompressionFilterSettings = CompressionFilterSettings?.ToTagValueFilterSettingsUpdate()
            };
        }

    }
}
