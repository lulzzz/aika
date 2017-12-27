using System;
using System.Collections.Generic;
using System.Text;

namespace Aika {
    public class ExceptionFilterResult : DataFilterResult {

        public ExceptionFilterResultDetails Result { get; }


        internal ExceptionFilterResult(DateTime utcReceivedAt, TagValue value, string notes, ExceptionFilterResultDetails result) : base(utcReceivedAt, value, notes) {
            Result = result ?? throw new ArgumentNullException(nameof(result));
        }

    }
}
