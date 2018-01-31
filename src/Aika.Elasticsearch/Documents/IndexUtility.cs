using System;
using System.Collections.Generic;
using System.Text;
using Nest;

namespace Aika.Elasticsearch.Documents {

    /// <summary>
    /// Utility class for creating NEST descriptors for Elasticsearch indices.
    /// </summary>
    public static class IndexUtility {

        /// <summary>
        /// Gets a create index descriptor for the tag definitions index.
        /// </summary>
        /// <param name="indexName">The index name.</param>
        /// <returns>
        /// A new <see cref="CreateIndexDescriptor"/>.
        /// </returns>
        public static CreateIndexDescriptor GetTagsIndexDescriptor(string indexName) {
            return new CreateIndexDescriptor(indexName).Mappings(x => x.Map<TagDocument>(m => m.AutoMap()));
        }


        /// <summary>
        /// Gets a create index descriptor for the tag definition change history index.
        /// </summary>
        /// <param name="indexName">The index name.</param>
        /// <returns>
        /// A new <see cref="CreateIndexDescriptor"/>.
        /// </returns>
        public static CreateIndexDescriptor GetTagChangeHistoryIndexDescriptor(string indexName) {
            return new CreateIndexDescriptor(indexName).Mappings(x => x.Map<TagChangeHistoryDocument>(m => m.AutoMap()));
        }


        /// <summary>
        /// Gets a create index descriptor for the state set definitions index.
        /// </summary>
        /// <param name="indexName">The index name.</param>
        /// <returns>
        /// A new <see cref="CreateIndexDescriptor"/>.
        /// </returns>
        public static CreateIndexDescriptor GetStateSetsIndexDescriptor(string indexName) {
            return new CreateIndexDescriptor(indexName).Mappings(x => x.Map<StateSetDocument>(m => m.AutoMap()));
        }


        /// <summary>
        /// Gets a create index descriptor for the index that stores snapshot and archive candidate values.
        /// </summary>
        /// <param name="indexName">The index name.</param>
        /// <returns>
        /// A new <see cref="CreateIndexDescriptor"/>.
        /// </returns>
        public static CreateIndexDescriptor GetSnapshotOrArchiveCandidateValuesIndexDescriptor(string indexName) {
            return new CreateIndexDescriptor(indexName).Mappings(x => x.Map<TagValueDocument>(m => m.AutoMap()));
        }


        /// <summary>
        /// Gets the index name to use when archiving a value for the specified sample time and tag definition.
        /// </summary>
        /// <param name="baseName">The base index name.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="utcSampleTime">The UTC sample time for the tag to be archived.</param>
        /// <param name="suffixGenerator">
        ///   A delegate function that can determine the suffix to use for the index.  Specify 
        ///   <see langword="null"/> to use the default suffix, which is the <paramref name="utcSampleTime"/> 
        ///   in <c>yyyy-MM</c> format (i.e. the year and month of the sample time).
        /// </param>
        /// <returns>
        /// The index name to use.
        /// </returns>
        public static string GetIndexNameForArchiveTagValue(string baseName, ElasticsearchTagDefinition tag, DateTime utcSampleTime, ArchiveIndexNameSuffixGenerator suffixGenerator) {
            var suffix = suffixGenerator == null
                ? utcSampleTime.ToString("yyyy-MM")
                : suffixGenerator.Invoke(tag, utcSampleTime);

            return baseName + suffix;
        }


        /// <summary>
        /// Gets a create index descriptor for an archive tag values index.
        /// </summary>
        /// <param name="indexName">The index name.</param>
        /// <returns>
        /// A new <see cref="CreateIndexDescriptor"/>.
        /// </returns>
        public static CreateIndexDescriptor GetArchiveValuesIndexDescriptor(string indexName) {
            return new CreateIndexDescriptor(indexName).Mappings(x => x.Map<TagValueDocument>(m => m.AutoMap()));
        }

    }
}
