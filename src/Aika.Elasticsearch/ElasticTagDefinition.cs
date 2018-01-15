using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aika.Elasticsearch
{
    public class ElasticTagDefinition : TagDefinition {

        private readonly ElasticHistorian _historian;

        internal Guid IdAsGuid { get; }

        internal IEnumerable<TagSecurity> Security { get; }


        internal ElasticTagDefinition(ElasticHistorian historian, Guid id, TagSettings settings, IEnumerable<TagSecurity> security, InitialTagValues initialTagValues, IEnumerable<TagChangeHistoryEntry> changeHistory) : base(historian, id.ToString(), settings, initialTagValues, changeHistory) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
            IdAsGuid = id;
            Security = security?.ToArray() ?? new TagSecurity[0];
        }


        protected override Task<WriteTagValuesResult> InsertArchiveValues(IEnumerable<TagValue> values, TagValue nextArchiveCandidate, CancellationToken cancellationToken) {
            return _historian.InsertArchiveValues(this, values, nextArchiveCandidate, cancellationToken);
        }


        protected override Task SaveSnapshotValue(TagValue value, CancellationToken cancellationToken) {
            return _historian.WriteSnapshotValue(this, value, cancellationToken);
        }
    }
}
