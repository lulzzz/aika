using System;
using System.Collections.Generic;
using System.Text;

namespace Aika {
    /// <summary>
    /// Describes an update to settings for exception or compression filtering on a tag.
    /// </summary>
    public class TagValueFilterSettingsUpdate {

        /// <summary>
        /// Gets or sets a flag indicating if the filter is enabled or not.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the filter limit type.
        /// </summary>
        public TagValueFilterDeviationType? LimitType { get; set; }

        /// <summary>
        /// Gets or sets the filter limit threshold.
        /// </summary>
        public double? Limit { get; set; }

        /// <summary>
        /// Gets or sets the filter window size.
        /// </summary>
        public TimeSpan? WindowSize { get; set; }


        /// <summary>
        /// Converts the object into an equivalent <see cref="TagValueFilterSettings"/> object.
        /// </summary>
        /// <returns>
        /// The equivalent <see cref="TagValueFilterSettings"/> object, with default values used for 
        /// fields in the <see cref="TagValueFilterSettingsUpdate"/> that do not specify a value.
        /// </returns>
        public TagValueFilterSettings ToTagValueFilterSettings() {
            return new TagValueFilterSettings(IsEnabled, LimitType ?? TagValueFilterDeviationType.Absolute, Limit ?? 0, WindowSize ?? TagValueFilterSettings.DefaultWindowSize);
        }

    }
}
