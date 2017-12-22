using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Aika.AspNetCore.Models.Query {
    /// <summary>
    /// Describes a historical data query.
    /// </summary>
    public class HistoricalDataRequest : IValidatableObject {

        /// <summary>
        /// Gets or sets the tags to query.
        /// </summary>
        [Required]
        [MinLength(1)]
        [MaxLength(100)]
        public string[] Tags { get; set; }

        /// <summary>
        /// Gets or sets the data function to use.  Common functions are defined in <see cref="Aika.Core.DataQueryFunctions"/>.
        /// </summary>
        [Required]
        public string Function { get; set; }

        /// <summary>
        /// Gets or sets the UTC query start time.
        /// </summary>
        [Required]
        public DateTime Start { get; set; }

        /// <summary>
        /// Gets or sets the UTC query end time.
        /// </summary>
        [Required]
        public DateTime End { get; set; }

        /// <summary>
        /// Gets or sets the query sample interval.
        /// </summary>
        public TimeSpan? SampleInterval { get; set; }

        /// <summary>
        /// Gets or sets the query point count.
        /// </summary>
        public int? PointCount { get; set; }


        /// <summary>
        /// Validates the object.
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>
        /// A collection of validation errors.
        /// </returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            if (Start >= End) {
                yield return new ValidationResult("Start time must be less than end time.", new[] { nameof(Start) });
            }

            if (!SampleInterval.HasValue && !PointCount.HasValue) {
                yield return new ValidationResult("Queries must specify either a sample interval, or a point count.", new[] { nameof(SampleInterval), nameof(PointCount) });
            }

            if (SampleInterval.HasValue && PointCount.HasValue) {
                yield return new ValidationResult("Queries can specified either a sample interval, or a point count, but not both.", new[] { nameof(SampleInterval), nameof(PointCount) });
            }

            if (SampleInterval.HasValue && SampleInterval.Value <= TimeSpan.Zero) {
                yield return new ValidationResult("Sample interval must be greater than zero.", new[] { nameof(SampleInterval) });
            }

            if (PointCount.HasValue && PointCount.Value <= 0) {
                yield return new ValidationResult("Point count must be greater than zero.", new[] { nameof(PointCount) });
            }
        }
    }

}
