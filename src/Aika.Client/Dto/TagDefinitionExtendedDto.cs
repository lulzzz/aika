using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Aika.Client.Dto {

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
        /// Gets or sets bespoke properties for the tag.
        /// </summary>
        public IDictionary<string, object> Properties { get; set; }

    }
}
