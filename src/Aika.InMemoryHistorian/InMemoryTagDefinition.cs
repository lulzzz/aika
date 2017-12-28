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


        internal InMemoryTagDefinition(InMemoryHistorian historian, ITaskRunner taskRunner, string id, string name, string description, string units, TagDataType dataType, string stateSet, TagValueFilterSettings exceptionFilterSettings, TagValueFilterSettings compressionFilterSettings, ILoggerFactory loggerFactory) : base(historian, taskRunner, id, name, description, units, dataType, stateSet, exceptionFilterSettings, compressionFilterSettings, null, null, null, null, loggerFactory) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
        }


        public override IDictionary<string, object> GetProperties() {
            return Properties.ToDictionary(x => x.Key, x => x.Value);
        }


        public override Task<TagValue> GetLastArchivedValue(CancellationToken cancellationToken) {
            var value = _historian.GetLastArchivedValue(Id);
            return Task.FromResult(value);
        }


        protected override Task<WriteTagValuesResult> OnInsertArchiveValues(IEnumerable<TagValue> values, CancellationToken cancellationToken) {
            var result = _historian.InsertArchiveValues(Id, values);
            return Task.FromResult(result);
        }

        
        internal new void OnDeleted() {
            base.OnDeleted();
        }
    }
}
