using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika;
using Microsoft.Extensions.Logging;

namespace Aika.Historians {
    public class InMemoryTagDefinition : TagDefinition {

        private readonly InMemoryHistorian _historian;

        internal ConcurrentDictionary<string, object> Properties { get; } = new ConcurrentDictionary<string, object>();


        internal InMemoryTagDefinition(InMemoryHistorian historian, string id, TagSettings tagSettings) : base(historian, id, tagSettings, null, null, null) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
        }


        public override IDictionary<string, object> GetProperties() {
            return Properties.ToDictionary(x => x.Key, x => x.Value);
        }


        protected override Task SaveSnapshotValue(TagValue value, CancellationToken cancellationToken) {
            _historian.SaveSnapshotValue(Id, value);
            return Task.CompletedTask;
        }


        protected override Task<WriteTagValuesResult> InsertArchiveValues(IEnumerable<TagValue> values, TagValue nextArchiveCandidate, CancellationToken cancellationToken) {
            _historian.SaveArchiveCandidateValue(Id, nextArchiveCandidate);
            var result = !(values?.Any() ?? false) 
                ? WriteTagValuesResult.CreateEmptyResult() 
                : _historian.InsertArchiveValues(Id, values);
            return Task.FromResult(result);
        }

        
        internal new void OnDeleted() {
            base.OnDeleted();
        }
    }
}
