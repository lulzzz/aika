using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Aika.Client.Dto {

    /// <summary>
    /// Base class that tag data request objects inherit from.
    /// </summary>
    public abstract class HistoricalDataRequest : IValidatableObject {

        /// <summary>
        /// Gets or sets the tags to query.
        /// </summary>
        [Required]
        [MinLength(1)]
        [MaxLength(100)]
        public string[] Tags { get; set; }

        /// <summary>
        /// Gets or sets the UTC query start time.
        /// </summary>
        [Required]
        public string Start { get; set; }

        /// <summary>
        /// Gets or sets the UTC query end time.
        /// </summary>
        [Required]
        public string End { get; set; }


        /// <summary>
        /// Validates the object.
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>
        /// A collection of validation errors.
        /// </returns>
        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            if (!Start.TryConvertToUtcDateTime(out var start)) {
                yield return new ValidationResult("Invalid start time.", new[] { nameof(Start) });
            }
            if (!End.TryConvertToUtcDateTime(out var end)) {
                yield return new ValidationResult("Invalid end time.", new[] { nameof(End) });
            }

            if (start >= end) {
                yield return new ValidationResult("Start time must be less than end time.", new[] { nameof(Start) });
            }
        }

    }
}
