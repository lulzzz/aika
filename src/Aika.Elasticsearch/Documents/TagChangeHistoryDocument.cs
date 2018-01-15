using System;
using System.Collections.Generic;
using System.Text;
using Nest;

namespace Aika.Elasticsearch.Documents {

    [ElasticsearchType(Name = "change", IdProperty = nameof(Id))]
    public class TagChangeHistoryDocument {

        public Guid Id { get; set; }

        public Guid TagId { get; set; }

        public DateTime UtcTime { get; set; }

        public string User { get; set; }

        public string Description { get; set; }

        public TagDocument PreviousVersion { get; set; }

    }
}
