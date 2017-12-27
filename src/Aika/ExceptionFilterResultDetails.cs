using System;
using System.Collections.Generic;
using System.Text;

namespace Aika {
    public class ExceptionFilterResultDetails {

        public bool Rejected { get; }

        public string Reason { get; }

        public TagValue LastExceptionValue { get; }

        public TagValueFilterSettings Settings { get; }

        public ExceptionLimits Limits { get; }


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
