using System;
using System.Collections.Generic;
using System.Text;
using Aika.Tags;
using Nest;

namespace Aika.Elasticsearch.Documents {

    /// <summary>
    /// Describes an Elasticsearch tag defintion.
    /// </summary>

    [ElasticsearchType(Name = "tag", IdProperty = nameof(Id))]
    public class TagDocument {

        /// <summary>
        /// Gets or sets the tag ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the tag name.
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
        /// Gets or sets the tag data type.
        /// </summary>
        public TagDataType DataType { get; set; }

        /// <summary>
        /// Gets or sets the name of the state set for the tag (when <see cref="DataType"/> is 
        /// <see cref="TagDataType.State"/>.
        /// </summary>
        public string StateSet { get; set; }

        /// <summary>
        /// Gets or sets the exception filter settings for the tag.
        /// </summary>
        public TagValueFilterSettingsUpdate ExceptionFilter { get; set; }

        /// <summary>
        /// Gets or sets the compression filter settings for the tag.
        /// </summary>
        public TagValueFilterSettingsUpdate CompressionFilter { get; set; }

        /// <summary>
        /// Gets or sets the tag security settings.
        /// </summary>
        public TagSecurity Security { get; set; }

        /// <summary>
        /// Gets or sets the tag metadata.
        /// </summary>
        public TagMetadata Metadata { get; set; }


        /// <summary>
        /// Metadata for tag documents.
        /// </summary>
        public class TagMetadata {

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


        /// <summary>
        /// Describes the security for a tag.
        /// </summary>
        public class TagSecurity {

            /// <summary>
            /// Gets or sets the tag's owner.
            /// </summary>
            public string Owner { get; set; }

            /// <summary>
            /// Gets or sets the tag's security policies.
            /// </summary>
            public IDictionary<string, TagSecurityPolicy> Policies { get; set; }

        }


        /// <summary>
        /// Describes a security policy for a tag.
        /// </summary>
        public class TagSecurityPolicy {

            /// <summary>
            /// Gets or sets the security entries that grant access to the policy.
            /// </summary>
            public TagSecurityEntry[] Allow { get; set; }

            /// <summary>
            /// Gets or sets the security entries that deny access to the policy.
            /// </summary>
            public TagSecurityEntry[] Deny { get; set; }

        }


        /// <summary>
        /// Describes a tag security policy entry.
        /// </summary>
        public class TagSecurityEntry {

            /// <summary>
            /// Gets or sets the claim type that the entry applies to.
            /// </summary>
            public string ClaimType { get; set; }

            /// <summary>
            /// Gets or sets the claim value that the entry applies to.
            /// </summary>
            public string Value { get; set; }

        }

    }

}
