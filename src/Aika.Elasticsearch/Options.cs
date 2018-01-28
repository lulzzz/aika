using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Elasticsearch {
    /// <summary>
    /// Options for the <see cref="ElasticsearchHistorian"/>.
    /// </summary>
    public class Options {

        /// <summary>
        /// Gets or sets the prefix to use for all indices.  If not specified, 
        /// <see cref="ElasticsearchHistorian.DefaultIndexPrefix"/> will be used.
        /// </summary>
        public string IndexPrefix { get; set; }


        /// <summary>
        /// Gets or sets a function that determines the index archive suffix to use when writing a 
        /// value for the specified tag and sample time.  If this property is <see langword="null"/>, 
        /// the index suffix will be derived from the UTC year and month of the sample time.
        /// </summary>
        public ArchiveIndexNameSuffixGenerator GetArchiveIndexSuffix { get; set; }

    }
}
