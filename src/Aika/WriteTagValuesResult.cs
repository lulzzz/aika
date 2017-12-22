using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aika {
    public class WriteTagValuesResult {

        public bool Success { get; }

        public int SampleCount { get; }

        public DateTime? UtcEarliestSampleTime { get; }

        public DateTime? UtcLatestSampleTime { get; }

        public IEnumerable<string> Notes { get; }


        public WriteTagValuesResult(bool success, int sampleCount, DateTime? utcEarliestSampleTime, DateTime? utcLatestSampleTime, IEnumerable<string> notes) {
            Success = success;
            SampleCount = sampleCount;
            UtcEarliestSampleTime = utcEarliestSampleTime;
            UtcLatestSampleTime = utcLatestSampleTime;
            Notes = notes?.ToArray() ?? new string[0];
        }


        public static WriteTagValuesResult CreateUnauthorizedResult() {
            return new WriteTagValuesResult(false, 0, null, null, new[] { "Unauthorized" });
        }

    }
}
