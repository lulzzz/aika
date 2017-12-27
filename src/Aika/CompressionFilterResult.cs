using System;
using System.Collections.Generic;
using System.Text;

namespace Aika {
    public class CompressionFilterResult : DataFilterResult {

        public CompressionFilterResultDetails Result { get; }


        internal CompressionFilterResult(DateTime utcReceivedAt, TagValue value, string notes, CompressionFilterResultDetails result) : base(utcReceivedAt, value, notes) {
            Result = result ?? throw new ArgumentNullException(nameof(result));
        }

    }
}
