using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aika.Tags {

    /// <summary>
    /// Describes the result of a write operation on a tag.
    /// </summary>
    public class WriteTagValuesResult {

        /// <summary>
        /// Gets a flag indicating if the write was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the number of samples that were written.
        /// </summary>
        public int SampleCount { get; }

        /// <summary>
        /// Gets the earliest UTC sample time that was written.
        /// </summary>
        public DateTime? UtcEarliestSampleTime { get; }

        /// <summary>
        /// Gets the latest UTC sample time that was written.
        /// </summary>
        public DateTime? UtcLatestSampleTime { get; }

        /// <summary>
        /// Gets notes associated with the write.
        /// </summary>
        public IEnumerable<string> Notes { get; }


        /// <summary>
        /// Creates a new <see cref="WriteTagValuesResult"/> object.
        /// </summary>
        /// <param name="success">A flag indicating if the write was successful.</param>
        /// <param name="sampleCount">The number of samples that were written.</param>
        /// <param name="utcEarliestSampleTime">The earliest UTC sample time that was written.</param>
        /// <param name="utcLatestSampleTime">The latest UTC sample time that was written.</param>
        /// <param name="notes">Notes associated with the write.</param>
        public WriteTagValuesResult(bool success, int sampleCount, DateTime? utcEarliestSampleTime, DateTime? utcLatestSampleTime, IEnumerable<string> notes) {
            Success = success;
            SampleCount = sampleCount;
            UtcEarliestSampleTime = utcEarliestSampleTime;
            UtcLatestSampleTime = utcLatestSampleTime;
            Notes = notes?.ToArray() ?? new string[0];
        }


        /// <summary>
        /// Creates a <see cref="WriteTagValuesResult"/> that represents an unauthorized attempt to write tag values.
        /// </summary>
        /// <returns>
        /// A new <see cref="WriteTagValuesResult"/> object.
        /// </returns>
        public static WriteTagValuesResult CreateUnauthorizedResult() {
            return new WriteTagValuesResult(false, 0, null, null, new[] { Resources.WriteTagValuesResult_Unauthorized });
        }


        /// <summary>
        /// Creates a <see cref="WriteTagValuesResult"/> that represents an attempt to write an empty set of values.
        /// </summary>
        /// <returns>
        /// A new <see cref="WriteTagValuesResult"/> object.
        /// </returns>
        public static WriteTagValuesResult CreateEmptyResult() {
            return new WriteTagValuesResult(false, 0, null, null, new[] { Resources.WriteTagValuesResult_NoValuesSpecified }); ;
        }

    }
}
