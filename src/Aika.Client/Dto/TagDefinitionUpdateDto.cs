using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;


namespace Aika.Client.Dto {
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
        public string DataType { get; set; }

        /// <summary>
        /// Gets or sets the name of the discrete state set to use for the tag, when <see cref="DataType"/> 
        /// is state-based.
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
        /// Gets or sets the description of the change.
        /// </summary>
        [Required]
        [MinLength(1)]
        [MaxLength(200)]
        public string ChangeDescription { get; set; }

    }
}
