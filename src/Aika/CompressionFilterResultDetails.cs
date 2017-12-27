using System;
using System.Collections.Generic;
using System.Text;

namespace Aika {
    public class CompressionFilterResultDetails {

        public bool Rejected { get; }

        public string Reason { get; }

        public TagValue LastArchivedValue { get; }

        public TagValue LastReceivedValue { get; }

        public TagValueFilterSettings Settings { get; }

        public CompressionLimits Limits { get; }


        internal CompressionFilterResultDetails(bool rejected, string reason, TagValue lastAchivedValue, TagValue lastReceivedValue, TagValueFilterSettings settings, CompressionLimits limits) {
            Rejected = rejected;
            Reason = reason;
            LastArchivedValue = lastAchivedValue;
            LastReceivedValue = lastReceivedValue;
            if (settings == null) {
                throw new ArgumentNullException(nameof(settings));
            }
            Settings = new TagValueFilterSettings(settings.IsEnabled, settings.LimitType, settings.Limit, settings.WindowSize);
            Limits = limits ?? throw new ArgumentNullException(nameof(limits));
        }


        public class CompressionLimits {

            /// <summary>
            /// Gets the compression limit set that was calculated when the last value was received by 
            /// the compression filter.  The <see cref="Incoming"/> limits are interpolated using the 
            /// last-archived tag value and the <see cref="Base"/> limit set.
            /// </summary>
            public CompressionLimitSet Base { get; }

            /// <summary>
            /// Gets the interpolated compression limits that were calculated for an incoming tag value.  
            /// The <see cref="Incoming"/> limits are interpolated using the last-archived tag value 
            /// and the <see cref="Base"/> limit set.
            /// </summary>
            /// </summary>
            public CompressionLimitSet Incoming { get; }

            /// <summary>
            /// Gets the updated compression limits that will be used the next time that the compression 
            /// filter receives an incoming value.
            /// </summary>
            public CompressionLimitSet Updated { get; }


            /// <summary>
            /// Creates a new <see cref="CompressionLimits"/> object.
            /// </summary>
            /// <param name="base">The compression limit set that the <paramref name="incoming"/> limits were inferred from.</param>
            /// <param name="incoming">The compression limit set that was calculated using the <paramref name="base"/> set and the last-archived tag value.</param>
            /// <param name="updated">The updated compression limit set that will be used the next time a value is received by the compression filter.</param>
            internal CompressionLimits(CompressionLimitSet @base, CompressionLimitSet incoming, CompressionLimitSet updated) {
                Base = @base;
                Incoming = incoming;
                Updated = updated;
            }

        }


        /// <summary>
        /// Describes a set of compression filter limits that, in combination with the last-archived 
        /// value for a tag, can be used to calculate the minimum and maxumum compression slopes that 
        /// an incoming value must exceed to pass through the compression filter.
        /// </summary>
        public class CompressionLimitSet {

            /// <summary>
            /// Gets the UTC sample time that the limits were calculated at.
            /// </summary>
            public DateTime UtcSampleTime { get; }

            /// <summary>
            /// Gets the minimum limit value at the <see cref="UtcSampleTime"/>.
            /// </summary>
            public double Minimum { get; }

            /// <summary>
            /// Gets the maximum limit value at the <see cref="UtcSampleTime"/>.
            /// </summary>
            public double Maximum { get; }


            /// <summary>
            /// Creates a new <see cref="CompressionLimitSet"/> object.
            /// </summary>
            /// <param name="utcSampleTime">The UTC sample time for the limits.</param>
            /// <param name="minimum">The minimum absolute limit value.</param>
            /// <param name="maximum">The maximum absolute limit value.</param>
            internal CompressionLimitSet(DateTime utcSampleTime, double minimum, double maximum) {
                UtcSampleTime = utcSampleTime;
                Minimum = minimum;
                Maximum = maximum;
            }

        }

    }
}
