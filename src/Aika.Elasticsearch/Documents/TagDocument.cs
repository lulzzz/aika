using System;
using System.Collections.Generic;
using System.Text;
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

        public TagSecurity[] Security { get; set; }

    }

}
