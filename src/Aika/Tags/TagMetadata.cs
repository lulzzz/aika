using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Tags {

    /// <summary>
    /// Metadata about a <see cref="TagDefinition"/>.
    /// </summary>
    public class TagMetadata {

        /// <summary>
        /// Gets the UTC creation time for the tag.
        /// </summary>
        public DateTime UtcCreatedAt { get; }

        /// <summary>
        /// Gets the identity of the tag's creator.
        /// </summary>
        public string Creator { get; }

        /// <summary>
        /// Gets the UTC last-modified time for the tag.
        /// </summary>
        public DateTime UtcLastModifiedAt { get; internal set; }

        /// <summary>
        /// The identity of the tag's last modifier.
        /// </summary>
        private string _lastModifiedBy;

        /// <summary>
        /// Gets the identity of the tag's last modifier.
        /// </summary>
        public string LastModifiedBy {
            get { return _lastModifiedBy; }
            internal set {
                _lastModifiedBy = String.IsNullOrWhiteSpace(value)
                    ? "<UNKNOWN>"
                    : value;
            }
        }


        /// <summary>
        /// Creates a new <see cref="TagMetadata"/> object.
        /// </summary>
        /// <param name="utcCreatedAt">The UTC created-at time for the tag.</param>
        /// <param name="creator">The identity of the tag's creator.</param>
        /// <param name="utcLastModifiedAt">The last-modified time for the tag.</param>
        /// <param name="lastModifiedBy">The identity of the tag's last modifier.</param>
        public TagMetadata(DateTime utcCreatedAt, string creator, DateTime? utcLastModifiedAt, string lastModifiedBy) {
            UtcCreatedAt = utcCreatedAt;
            Creator = String.IsNullOrWhiteSpace(creator)
                ? "<UNKNOWN>"
                : creator;
            UtcLastModifiedAt = utcLastModifiedAt ?? utcCreatedAt;
            LastModifiedBy = String.IsNullOrWhiteSpace(lastModifiedBy) 
                ? creator 
                : lastModifiedBy;
        }


        /// <summary>
        /// Creates a new <see cref="TagMetadata"/> object.
        /// </summary>
        /// <param name="utcCreatedAt">The UTC created-at time for the tag.</param>
        /// <param name="creator">The identity of the tag's creator.</param>
        public TagMetadata(DateTime utcCreatedAt, string creator) : this(utcCreatedAt, creator, null, null) { }

    }
}
