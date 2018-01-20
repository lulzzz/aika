using System;
using System.Globalization;
using System.Linq;

namespace Aika {

    /// <summary>
    /// Describes a tag value at a single point in time.
    /// </summary>
    public class TagValue : IEquatable<TagValue> {

        /// <summary>
        /// Gets the UTC sample time for the value.
        /// </summary>
        public DateTime UtcSampleTime { get; }

        /// <summary>
        /// Gets the numeric value, if defined.
        /// </summary>
        public double NumericValue { get; }

        /// <summary>
        /// Gets the text value, if defined.
        /// </summary>
        public string TextValue { get; }

        /// <summary>
        /// Gets a flag indicating if the value is numeric or not.  Non-numeric values have a 
        /// <see cref="NumericValue"/> property this is equal to <see cref="Double.NaN"/>, 
        /// <see cref="Double.NegativeInfinity"/>, or <see cref="Double.PositiveInfinity"/>.
        /// </summary>
        public bool IsNumeric { get; }

        /// <summary>
        /// Gets the quality state of the value.
        /// </summary>
        public TagValueQuality Quality { get; }

        /// <summary>
        /// Gets the unit of measurement for the value.
        /// </summary>
        public string Units { get; }


        /// <summary>
        /// Creates a new <see cref="TagValue"/> object.
        /// </summary>
        /// <param name="utcSampleTime">The UTC sample time.</param>
        /// <param name="numericValue">The numeric value.</param>
        /// <param name="textValue">The text value.</param>
        /// <param name="quality">The quality status for the value.</param>
        /// <param name="units">The unit of measure for the tag value.</param>
        public TagValue(DateTime utcSampleTime, double numericValue, string textValue, TagValueQuality quality, string units) {
            UtcSampleTime = utcSampleTime;
            NumericValue = numericValue;
            IsNumeric = !Double.IsNaN(numericValue) && !Double.IsInfinity(numericValue);
            TextValue = String.IsNullOrWhiteSpace(textValue)
                            ? numericValue.ToString(CultureInfo.InvariantCulture)
                            : textValue;
            Quality = quality;
            Units = String.IsNullOrWhiteSpace(units)
                       ? String.Empty
                       : units;
        }


        /// <summary>
        /// Creates a new <see cref="TagValue"/> that represents a message saying that a caller is not 
        /// authorized to read data from a tag.
        /// </summary>
        /// <param name="utcSampleTime">The sample time to use for the <see cref="TagValue"/>.</param>
        /// <returns>
        /// A new <see cref="TagValue"/> object.
        /// </returns>
        public static TagValue CreateUnauthorizedTagValue(DateTime utcSampleTime) {
            return new TagValue(utcSampleTime, Double.NaN, Resources.TagValue_Unauthorized, TagValueQuality.Bad, null);
        }


        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// The hash code.
        /// </returns>
        public override int GetHashCode() {
            return UtcSampleTime.GetHashCode();
        }


        /// <summary>
        /// Tests if the specified object is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to test.</param>
        /// <returns>
        /// <see langword="true"/> if the objects are equal; otherwise, <see langword="false"/>.
        /// </returns>
        public override bool Equals(object obj) {
            return Equals(obj as TagValue);
        }


        /// <summary>
        /// Tests if the specified tag value is equal to this instance.
        /// </summary>
        /// <param name="other">The object to test.</param>
        /// <returns>
        /// <see langword="true"/> if the objects are equal; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Equals(TagValue other) {
            if (other == null) {
                return false;
            }

            return UtcSampleTime == other.UtcSampleTime &&
                   NumericValue == other.NumericValue &&
                   TextValue == other.TextValue
                   && Quality == other.Quality;
        }
    }
}
