using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Aika;

namespace Aika.AspNetCore.Models.Query {

    /// <summary>
    /// Web API model for a tag value.
    /// </summary>
    public class TagValueDto {

        /// <summary>
        /// Gets the UTC sample time for the value.
        /// </summary>
        [Required]
        public DateTime UtcSampleTime { get; set; }

        /// <summary>
        /// Gets the numeric value, if defined.
        /// </summary>
        public double? NumericValue { get; set; }

        /// <summary>
        /// Gets the text value, if defined.
        /// </summary>
        public string TextValue { get; set; }

        /// <summary>
        /// Gets a flag indicating if the value is numeric or not.  Non-numeric values have a 
        /// <see cref="NumericValue"/> property this is equal to <see cref="Double.NaN"/>, 
        /// <see cref="Double.NegativeInfinity"/>, or <see cref="Double.PositiveInfinity"/>.
        /// </summary>
        public bool IsNumeric {
            get { return NumericValue.HasValue && !Double.IsNaN(NumericValue.Value) && !Double.IsInfinity(NumericValue.Value); }
        }

        /// <summary>
        /// Gets the quality state of the value.
        /// </summary>
        public TagValueQuality Quality { get; set; }

        /// <summary>
        /// Gets the unit of measurement for the value.
        /// </summary>
        public string Units { get; }


        /// <summary>
        /// Creates a new <see cref="TagValueDto"/> object.
        /// </summary>
        public TagValueDto() { }


        /// <summary>
        /// Creates a new <see cref="TagValueDto"/> object using the specified <see cref="TagValue"/> as a template.
        /// </summary>
        /// <param name="value">The <see cref="TagValue"/> to copy from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        internal TagValueDto(TagValue value) : this() {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }

            UtcSampleTime = value.UtcSampleTime;
            NumericValue = value.NumericValue;
            TextValue = value.TextValue;
            Quality = value.Quality;
            Units = value.Units;
        }


        /// <summary>
        /// Converts the <see cref="TagValueDto"/> into a <see cref="TagValue"/> instance.
        /// </summary>
        /// <returns>
        /// The equivalent <see cref="TagValue"/> instance.
        /// </returns>
        internal TagValue ToTagValue() {
            return new TagValue(UtcSampleTime, NumericValue ?? Double.NaN, TextValue, Quality, Units);
        }

    }
}
