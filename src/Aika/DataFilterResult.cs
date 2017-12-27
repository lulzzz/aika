using System;

namespace Aika {

    /// <summary>
    /// Describes a value that was processed by a <see cref="DataFilter"/>.
    /// </summary>
    public abstract class DataFilterResult {

        /// <summary>
        /// Gets the UTC time stamp that the value was received at.
        /// </summary>
        public DateTime UtcReceivedAt { get; }

        /// <summary>
        /// Gets the tag value.
        /// </summary>
        public TagValue Value { get; }

        /// <summary>
        /// Gets additional notes about the result.
        /// </summary>
        public string Notes { get; }


        /// <summary>
        /// Creates a new <see cref="DataFilterResult"/> object.
        /// </summary>
        /// <param name="utcReceivedAt">The UTC time that the value was received at.</param>
        /// <param name="value">The value.</param>
        /// <param name="notes">Additional notes about the result.</param>
        internal DataFilterResult(DateTime utcReceivedAt, TagValue value, string notes) {
            UtcReceivedAt = utcReceivedAt;
            Value = value;
            Notes = notes;
        }

    }

}
