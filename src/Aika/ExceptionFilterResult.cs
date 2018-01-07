using System;
using System.Collections.Generic;
using System.Text;

namespace Aika {
    /// <summary>
    /// Describes the result of an exception filter data processing operation.
    /// </summary>
    public class ExceptionFilterResult : DataFilterResult {

        /// <summary>
        /// Gets the result details.
        /// </summary>
        public ExceptionFilterResultDetails Result { get; }


        /// <summary>
        /// Creates a new <see cref="ExceptionFilterResult"/> object.
        /// </summary>
        /// <param name="utcReceivedAt">The UTC time that the incoming value was received at.</param>
        /// <param name="value">The received value.</param>
        /// <param name="notes">Notes associated with the value.</param>
        /// <param name="result">The result details.</param>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
        internal ExceptionFilterResult(DateTime utcReceivedAt, TagValue value, string notes, ExceptionFilterResultDetails result) : base(utcReceivedAt, value, notes) {
            Result = result ?? throw new ArgumentNullException(nameof(result));
        }

    }
}
