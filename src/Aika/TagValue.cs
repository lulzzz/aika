using System;
using System.Globalization;

namespace Aika {

    /// <summary>
    /// Describes a tag value at a single point in time.
    /// </summary>
    public class TagValue {

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
        /// <param name="tagName">The tag name.</param>
        /// <param name="utcSampleTime">The UTC sample time.</param>
        /// <param name="numericValue">The numeric value.</param>
        /// <param name="textValue">The text value.</param>
        /// <param name="quality">The quality status for the value.</param>
        /// <param name="units">The unit of measure for the tag value.</param>
        /// <param name="notes">The ntoes associated with the value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tagName"/> is <see langword="null"/>.</exception>
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
            return new TagValue(utcSampleTime, Double.NaN, "Unauthorized", TagValueQuality.Bad, null);
        }

    }
}
