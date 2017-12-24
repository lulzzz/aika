using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Aika.AspNetCore.Models.Query {
    /// <summary>
    /// Describes a historical data query.
    /// </summary>
    public class AggregatedDataRequest : HistoricalDataRequest {

        /// <summary>
        /// Gets or sets the aggregate function to use.
        /// </summary>
        [Required]
        public string Function { get; set; }


        /// <summary>
        /// Gets or sets the query sample interval.
        /// </summary>
        public string SampleInterval { get; set; }

        /// <summary>
        /// Gets or sets the query point count.
        /// </summary>
        [Range(1, Int32.MaxValue)]
        public int? PointCount { get; set; }


        /// <summary>
        /// Validates the object.
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>
        /// A collection of validation errors.
        /// </returns>
        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            foreach (var item in base.Validate(validationContext)) {
                yield return item;
            }

            if (SampleInterval == null && !PointCount.HasValue) {
                yield return new ValidationResult("Queries must specify either a sample interval, or a point count.", new[] { nameof(SampleInterval), nameof(PointCount) });
            }

            if (SampleInterval != null && PointCount.HasValue) {
                yield return new ValidationResult("Queries can specified either a sample interval, or a point count, but not both.", new[] { nameof(SampleInterval), nameof(PointCount) });
            }

            if (SampleInterval != null) {
                if (!SampleInterval.TryConvertToTimeSpan(out var sampleInterval)) {
                    yield return new ValidationResult("Invalid sample interval.", new[] { nameof(SampleInterval) });
                }
                else if (sampleInterval <= TimeSpan.Zero) {
                    yield return new ValidationResult("Sample interval must be greater than zero.", new[] { nameof(SampleInterval) });
                }
            }
        }
    }

}
