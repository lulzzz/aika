using System;
using System.Collections.Generic;
using System.Text;
using Nest;

namespace Aika.Elasticsearch.Documents {

    /// <summary>
    /// Describes an Elasticsearch tag change history document.
    /// </summary>
    [ElasticsearchType(Name = "change", IdProperty = nameof(Id))]
    public class TagChangeHistoryDocument {

        /// <summary>
        /// Gets or sets the document ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the tag ID that the change document applies to.
        /// </summary>
        public Guid TagId { get; set; }

        /// <summary>
        /// Gets or sets the UTC time that the change was made at.
        /// </summary>
        public DateTime UtcTime { get; set; }

        /// <summary>
        /// Gets or sets the identity of the user who made the change.
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Gets or sets the description of the change.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the previous version of the tag definition.
        /// </summary>
        public TagDocument PreviousVersion { get; set; }

    }
}
