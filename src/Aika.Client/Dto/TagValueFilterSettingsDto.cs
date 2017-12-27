using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Aika.Client.Dto {
    /// <summary>
    /// Describes the settings for an exception or compression filter for a tag.
    /// </summary>
    public class TagValueFilterSettingsDto : IValidatableObject {

        /// <summary>
        /// Gets or sets a flag indicating if the filter is enabled or not.
        /// </summary>
        [Required]
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the filter limit type.
        /// </summary>
        public string LimitType { get; set; }

        /// <summary>
        /// Gets or sets the filter limit threshold.
        /// </summary>
        public double? Limit { get; set; }

        /// <summary>
        /// Gets or sets the filter window size.
        /// </summary>
        public TimeSpan? WindowSize { get; set; }


        /// <summary>
        /// Validates the object.
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>
        /// A collection of validation errors.
        /// </returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            if (IsEnabled) {
                if (String.IsNullOrWhiteSpace(LimitType)) {
                    yield return new ValidationResult("Limit type is required when the filter is enabled.", new[] { nameof(LimitType) });
                }
                if (!Limit.HasValue) {
                    yield return new ValidationResult("Limit is required when the filter is enabled.", new[] { nameof(Limit) });
                }
                if (Limit.HasValue && Limit.Value < 0) {
                    yield return new ValidationResult("Limit must be greater than or equal to zero.", new[] { nameof(Limit) });
                }
                if (!WindowSize.HasValue) {
                    yield return new ValidationResult("Window size is required when the filter is enabled.", new[] { nameof(WindowSize) });
                }
                if (WindowSize.HasValue && WindowSize.Value <= TimeSpan.Zero) {
                    yield return new ValidationResult("The window size for the filter must be a positive time span.", new[] { nameof(WindowSize) });
                }
            }
        }
    }
}
