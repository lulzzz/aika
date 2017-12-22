using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aika.AspNetCore.Models.Write {

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


        /// <summary>
        /// Creates a new <see cref="WriteTagValuesResultDto"/> object.
        /// </summary>
        public WriteTagValuesResultDto() { }


        /// <summary>
        /// Creates a new <see cref="WriteTagValuesResultDto"/> object using the specified <see cref="WriteTagValuesResult"/>.
        /// </summary>
        /// <param name="result">The <see cref="WriteTagValuesResult"/> to copy from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
        internal WriteTagValuesResultDto(WriteTagValuesResult result) : this() {
            if (result == null) {
                throw new ArgumentNullException(nameof(result));
            }

            Success = result.Success;
            SampleCount = result.SampleCount;
            UtcEarliestSampleTime = result.UtcEarliestSampleTime;
            UtcLatestSampleTime = result.UtcLatestSampleTime;
            Notes = result.Notes?.ToArray();
        }

    }
}
