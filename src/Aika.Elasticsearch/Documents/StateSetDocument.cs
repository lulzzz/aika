using System;
using System.Collections.Generic;
using System.Text;
using Nest;

namespace Aika.Elasticsearch.Documents {

    /// <summary>
    /// Describes an Elasticsearch state set document.
    /// </summary>
    [ElasticsearchType(Name = "stateSet", IdProperty = nameof(Name))]
    public class StateSetDocument {

        /// <summary>
        /// Gets or sets the state set name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the state set description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the state set definitions.
        /// </summary>
        public StateSetEntryDocument[] States { get; set; }

    }


    /// <summary>
    /// Describes a state definition in a <see cref="StateSetDocument"/>.
    /// </summary>
    public class StateSetEntryDocument {

        /// <summary>
        /// Gets or sets the state name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the state value.
        /// </summary>
        public int Value { get; set; }

    }
}
