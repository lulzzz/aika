using System;
using System.Collections.Generic;
using System.Text;
using Aika.Tags;

namespace Aika.DataFilters {
    /// <summary>
    /// Describes the detailed result of a compression filter data processing operation. 
    /// </summary>
    public class CompressionFilterResultDetails {

        /// <summary>
        /// Gets a flag indicating if the incoming value was rejected or not.  Values that are not 
        /// rejected result in a value being sent to the tag's data archive.
        /// </summary>
        public bool Rejected { get; }

        /// <summary>
        /// Gets the reason that the incoming value was (or wasn't) rejected.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Gets the last-archived value for the tag at the point that incoming value was received.
        /// </summary>
        public TagValue LastArchivedValue { get; }

        /// <summary>
        /// Gets the last-received value for the tag at the point that the incoming value was received 
        /// (i.e. the value that was received by the compression filter immediately before the incoming 
        /// value).
        /// </summary>
        public TagValue LastReceivedValue { get; }

        /// <summary>
        /// Gets the compression filter settings when the incoming value was received.
        /// </summary>
        public TagValueFilterSettings Settings { get; }

        /// <summary>
        /// Gets the compression limit values that were applied to the incoming value.
        /// </summary>
        public CompressionLimits Limits { get; }


        /// <summary>
        /// Creates a new <see cref="CompressionFilterResultDetails"/> object.
        /// </summary>
        /// <param name="rejected">Flags if the incoming value was rejected by the filter or not.</param>
        /// <param name="reason">The reason that the incoming value was (or wasn't) rejected.</param>
        /// <param name="lastAchivedValue">
        ///   The last-archived value for the tag at the point that incoming value was received.
        /// </param>
        /// <param name="lastReceivedValue">
        ///   The last-received value for the tag at the point that the incoming value was received 
        ///   (i.e. the value that was received by the compression filter immediately before the 
        ///   incoming value).
        /// </param>
        /// <param name="settings">The compression filter settings when the incoming value was received.</param>
        /// <param name="limits">The compression limit values that were applied to the incoming value.</param>
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


        /// <summary>
        /// Describes the limits applied to an incoming tag value by a compression filter.
        /// </summary>
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
