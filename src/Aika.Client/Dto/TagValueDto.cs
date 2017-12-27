using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Aika;

namespace Aika.Client.Dto {

    /// <summary>
    /// Describes a tag value.
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
        public string Quality { get; set; }

        /// <summary>
        /// Gets the unit of measurement for the value.
        /// </summary>
        public string Units { get; }

    }
}
