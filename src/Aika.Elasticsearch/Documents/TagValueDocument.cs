using System;
using System.Collections.Generic;
using System.Text;
using Aika.Tags;
using Nest;

namespace Aika.Elasticsearch.Documents {

    /// <summary>
    /// Describes an Elasticsearch tag value document.
    /// </summary>
    [ElasticsearchType(Name = "tagValue", IdProperty = nameof(Id))]
    public class TagValueDocument {

        /// <summary>
        /// Gets or sets the document ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the tag ID.
        /// </summary>
        public Guid TagId { get; set; }

        /// <summary>
        /// Gets or sets the UTC sample time for the value.
        /// </summary>
        public DateTime UtcSampleTime { get; set; }

        /// <summary>
        /// Gets or sets the tag value's numeric value.
        /// </summary>
        public double NumericValue { get; set; }

        /// <summary>
        /// Gets or sets the tag value's text value.
        /// </summary>
        public string TextValue { get; set; }

        /// <summary>
        /// Gets or sets the tag value's quality status.
        /// </summary>
        public TagValueQuality Quality { get; set; }

        /// <summary>
        /// Gets or sets additional properties associated with the value.
        /// </summary>
        public IDictionary<string, object> Properties { get; set; }

    }
}
