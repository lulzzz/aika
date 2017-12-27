using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aika.Client.Dto {

    /// <summary>
    /// Describes the result of a write operation.
    /// </summary>
    public class WriteTagValuesResultDto {

        /// <summary>
        /// Gets or sets a flag indicating if the write was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the number of samples that were written.
        /// </summary>
        public int SampleCount { get; set; }

        /// <summary>
        /// Gets or sets the eariest UTC sample time that was written.
        /// </summary>
        public DateTime? UtcEarliestSampleTime { get; set; }

        /// <summary>
        /// Gets or sets the latest UTC sample time that was written.
        /// </summary>
        public DateTime? UtcLatestSampleTime { get; set; }

        /// <summary>
        /// Gets or sets the notes associated with the write.
        /// </summary>
        public IEnumerable<string> Notes { get; set; }

    }
}
