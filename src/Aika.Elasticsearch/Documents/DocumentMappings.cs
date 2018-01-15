using System;
using System.Collections.Generic;
using System.Text;
using Nest;

namespace Aika.Elasticsearch.Documents {
    public static class DocumentMappings {

        public static CreateIndexDescriptor GetTagsIndexDescriptor(string indexName) {
            return new CreateIndexDescriptor(indexName).Mappings(x => x.Map<TagDocument>(m => m.AutoMap()));
        }


        public static CreateIndexDescriptor GetTagChangeHistoryIndexDescriptor(string indexName) {
            return new CreateIndexDescriptor(indexName).Mappings(x => x.Map<TagChangeHistoryDocument>(m => m.AutoMap()));
        }


        public static CreateIndexDescriptor GetStateSetsIndexDescriptor(string indexName) {
            return new CreateIndexDescriptor(indexName).Mappings(x => x.Map<StateSetDocument>(m => m.AutoMap()));
        }


        public static CreateIndexDescriptor GetSnapshotOrArchiveCandidateValuesIndexDescriptor(string indexName) {
            return new CreateIndexDescriptor(indexName).Mappings(x => x.Map<TagValueDocument>(m => m.AutoMap()));
        }


        public static string GetIndexNameForArchiveTagValue(string baseName, DateTime utcSampleTime) {
            return $"{baseName}{utcSampleTime:yyyy-MM}";
        }


        public static CreateIndexDescriptor GetArchiveValuesIndexDescriptor(string indexName) {
            return new CreateIndexDescriptor(indexName).Mappings(x => x.Map<TagValueDocument>(m => m.AutoMap()));
        }

    }
}
