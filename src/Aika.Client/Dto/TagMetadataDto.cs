using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Client.Dto {
    /// <summary>
    /// Metadata for tag documents.
    /// </summary>
    public class TagMetadataDto {

        /// <summary>
        /// Gets the UTC creation time for the tag.
        /// </summary>
        public DateTime UtcCreatedAt { get; set; }

        /// <summary>
        /// Gets the identity of the tag's creator.
        /// </summary>
        public string Creator { get; set; }

        /// <summary>
        /// Gets the UTC last-modified time for the tag.
        /// </summary>
        public DateTime UtcLastModifiedAt { get; set; }

        /// <summary>
        /// Gets the identity of the tag's last modifier.
        /// </summary>
        public string LastModifiedBy { get; set; }
    }
}
