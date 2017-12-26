using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Aika.AspNetCore.Models {

    /// <summary>
    /// Describes an extended-detail tag definition.
    /// </summary>
    public class TagDefinitionExtendedDto : TagDefinitionDto {

        /// <summary>
        /// Gets or sets the exception filter settings for the tag.
        /// </summary>
        public TagValueFilterSettingsDto ExceptionFilterSettings { get; set; }

        /// <summary>
        /// Gets or sets the compression filter settings for the tag.
        /// </summary>
        public TagValueFilterSettingsDto CompressionFilterSettings { get; set; }

        /// <summary>
        /// Gets or sets the UTC time that the tag was created at.
        /// </summary>
        public DateTime UtcCreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the UTC time that the tag was last modified at.
        /// </summary>
        public DateTime UtcLastModifiedAt { get; set; }


        /// <summary>
        /// Creates a new <see cref="TagDefinitionExtendedDto"/> object.
        /// </summary>
        public TagDefinitionExtendedDto() : base() { }


        /// <summary>
        /// Creates a new <see cref="TagDefinitionExtendedDto"/> object from the specified tag definition.
        /// </summary>
        /// <param name="tagDefinition">The tag definition.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tagDefinition"/> is <see langword="null"/>.</exception>
        internal TagDefinitionExtendedDto(TagDefinition tagDefinition) : base(tagDefinition) {
            ExceptionFilterSettings = new TagValueFilterSettingsDto(tagDefinition.ExceptionFilterSettings);
            CompressionFilterSettings = new TagValueFilterSettingsDto(tagDefinition.CompressionFilterSettings);
            UtcCreatedAt = tagDefinition.UtcCreatedAt;
            UtcLastModifiedAt = tagDefinition.UtcLastModifiedAt;
        }


    }
}
