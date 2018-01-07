using System;
using System.Collections.Generic;
using System.Text;

namespace Aika {
    /// <summary>
    /// Describes the detailed result of an exception filter data processing operation. 
    /// </summary>
    public class ExceptionFilterResultDetails {

        /// <summary>
        /// Gets a flag indicating if the incoming value was rejected or not.  Values that are not 
        /// rejected result in the incoming value and the previous incoming value being passed to 
        /// the tag's compression filter.
        /// </summary>
        public bool Rejected { get; }

        /// <summary>
        /// Gets the reason that the incoming value was (or wasn't) rejected.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Gets the last exception value for the tag at the point that the incoming value was received.
        /// </summary>
        public TagValue LastExceptionValue { get; }

        /// <summary>
        /// Gets the exception filter settings when the incoming value was received.
        /// </summary>
        public TagValueFilterSettings Settings { get; }

        /// <summary>
        /// Gets the exception limit values that were applied to the incoming value.
        /// </summary>
        public ExceptionLimits Limits { get; }


        /// <summary>
        /// Creates a new <see cref="ExceptionFilterResultDetails"/> object.
        /// </summary>
        /// <param name="rejected">Flags if the incoming value was rejected by the filter or not.</param>
        /// <param name="reason">The reason that the incoming value was (or wasn't) rejected.</param>
        /// <param name="lastExceptionValue">
        ///   The last exception value for the tag at the point that the incoming value was received.
        /// </param>
        /// <param name="settings">The compression filter settings when the incoming value was received.</param>
        /// <param name="limits">The compression limit values that were applied to the incoming value.</param>
        internal ExceptionFilterResultDetails(bool rejected, string reason, TagValue lastExceptionValue, TagValueFilterSettings settings, ExceptionLimits limits) {
            Rejected = rejected;
            Reason = reason;
            LastExceptionValue = lastExceptionValue;
            Settings = new TagValueFilterSettings(settings.IsEnabled, settings.LimitType, settings.Limit, settings.WindowSize);
            Limits = limits;
        }


        /// <summary>
        /// Describes a set of compression filter limits that, in combination with the last-archived 
        /// value for a tag, can be used to calculate the minimum and maxumum compression slopes that 
        /// an incoming value must exceed to pass through the compression filter.
        /// </summary>
        public class ExceptionLimits {

            /// <summary>
            /// Gets the absolute minimum limit value used.
            /// </summary>
            public double Minimum { get; }

            /// <summary>
            /// Gets the absolute maximum limit value used.
            /// </summary>
            public double Maximum { get; }


            /// <summary>
            /// Creates a new <see cref="ExceptionLimits"/> object.
            /// </summary>
            /// <param name="minimum">The minimum absolute limit value.</param>
            /// <param name="maximum">The maximum absolute limit value.</param>
            internal ExceptionLimits(double minimum, double maximum) {
                Minimum = minimum;
                Maximum = maximum;
            }

        }

    }
}
