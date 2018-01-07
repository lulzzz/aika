using System;
using System.Globalization;
using System.Linq;

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
        /// Attempts to create a new <see cref="TagValue"/> from a <see cref="StateSet"/>, based on an existing value.
        /// </summary>
        /// <param name="stateSet">The state set to use.</param>
        /// <param name="template">The value to copy from.</param>
        /// <param name="value">A new <see cref="TagValue"/> that uses the correct name and numeric value of the matching state.</param>
        /// <returns>
        /// A flag that indicates if <paramref name="template"/> could be mapped to a state in the 
        /// <paramref name="stateSet"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="stateSet"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="template"/> is <see langword="null"/>.</exception>
        public static bool TryCreateFromStateSet(StateSet stateSet, TagValue template, out TagValue value) {
            if (stateSet == null) {
                throw new ArgumentNullException(nameof(stateSet));
            }
            if (template == null) {
                throw new ArgumentNullException(nameof(template));
            }

            StateSetItem state = null;
            if (!String.IsNullOrWhiteSpace(template.TextValue)) {
                state = stateSet[template.TextValue];
            }

            if (state == null) {
                var valAsInt = (int) template.NumericValue;
                state = stateSet.States.FirstOrDefault(x => x.Value == valAsInt);
            }

            if (state == null) {
                value = null;
                return false;
            }

            value = new TagValue(template.UtcSampleTime, state.Value, state.Name, template.Quality, null);
            return true;
        }

    }
}
