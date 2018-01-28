using System;
using System.Collections.Generic;
using System.Text;
using Aika.Tags;
using Nest;

namespace Aika.Elasticsearch.Documents {

    [ElasticsearchType(Name = "tag", IdProperty = nameof(Id))]
    public class TagDocument {

        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Units { get; set; }

        public TagDataType DataType { get; set; }

        public string StateSet { get; set; }

        public TagValueFilterSettingsUpdate ExceptionFilter { get; set; }

        public TagValueFilterSettingsUpdate CompressionFilter { get; set; }

        public TagSecurity Security { get; set; }

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


        public class TagSecurity {

            public string Owner { get; set; }

            public IDictionary<string, TagSecurityPolicy> Policies { get; set; }

        }


        public class TagSecurityPolicy {

            public TagSecurityEntry[] Allow { get; set; }

            public TagSecurityEntry[] Deny { get; set; }

        }

        public class TagSecurityEntry {

            public string ClaimType { get; set; }

            public string Value { get; set; }

        }

    }

}
