using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Elasticsearch {

    /// <summary>
    /// Delegate that is used to generate the Elasticsearch archive index suffix to use when writing 
    /// data for the specified tag and sample time combination.
    /// </summary>
    /// <param name="tag">The tag.</param>
    /// <param name="utcSampleTime">The UTC sample time.</param>
    /// <returns>
    /// The archive index suffix to use.
    /// </returns>
    public delegate string ArchiveIndexNameSuffixGenerator(ElasticsearchTagDefinition tag, DateTime utcSampleTime);
    
}
