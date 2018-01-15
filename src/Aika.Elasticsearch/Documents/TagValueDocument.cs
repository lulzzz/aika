using System;
using System.Collections.Generic;
using System.Text;
using Nest;

namespace Aika.Elasticsearch.Documents {

    [ElasticsearchType(Name = "tagValue", IdProperty = nameof(Id))]
    public class TagValueDocument {

        public Guid Id { get; set; }

        public Guid TagId { get; set; }

        public DateTime UtcSampleTime { get; set; }

        public double NumericValue { get; set; }

        public string TextValue { get; set; }

        public TagValueQuality Quality { get; set; }

    }
}
