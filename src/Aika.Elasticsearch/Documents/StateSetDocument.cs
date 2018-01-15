using System;
using System.Collections.Generic;
using System.Text;
using Nest;

namespace Aika.Elasticsearch.Documents {

    [ElasticsearchType(Name = "stateSet", IdProperty = nameof(Name))]
    public class StateSetDocument {

        public string Name { get; set; }

        public string Description { get; set; }

        public StateSetEntryDocument[] States { get; set; }

    }


    public class StateSetEntryDocument {

        public string Name { get; set; }

        public int Value { get; set; }

    }
}
