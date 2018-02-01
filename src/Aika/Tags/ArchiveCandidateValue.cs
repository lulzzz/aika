using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Tags {

    /// <summary>
    /// Describes an archive candidate value.  That is, a tag value that will be written permanently 
    /// to the historian's data archive the next time an incoming value passes the tag's exception 
    /// and compression filters.
    /// </summary>
    public class ArchiveCandidateValue {

        /// <summary>
        /// Gets the tag value.
        /// </summary>
        public TagValue Value { get; }

        /// <summary>
        /// Gets the minimum compression angle value that was calculated when the <see cref="Value"/> 
        /// was received by the tag's compression filter.
        /// </summary>
        public double CompressionAngleMinimum { get; }

        /// <summary>
        /// Gets the minimum compression angle value that was calculated when the <see cref="Value"/> 
        /// was received by the tag's compression filter.
        /// </summary>
        public double CompressionAngleMaximum { get; }


        /// <summary>
        /// Creates a new <see cref="ArchiveCandidateValue"/> object.
        /// </summary>
        /// <param name="value">The tag value.</param>
        /// <param name="compressionAngleMinimum">
        ///   The minimum compression angle value that was calculated when the <paramref name="value"/>
        ///   was received by the tag's compression filter.
        /// </param>
        /// <param name="compressionAngleMaximum">
        ///   The minimum compression angle value that was calculated when the <paramref name="value"/>
        ///   was received by the tag's compression filter.
        /// </param>
        public ArchiveCandidateValue(TagValue value, double compressionAngleMinimum, double compressionAngleMaximum) {
            Value = value;
            CompressionAngleMinimum = compressionAngleMinimum;
            CompressionAngleMaximum = compressionAngleMaximum;
        }

    }
}
