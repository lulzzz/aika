using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Tags {

    /// <summary>
    /// Describes settings for exception or compression filtering on a tag.
    /// </summary>
    public class TagValueFilterSettings {

        /// <summary>
        /// The default window size for an exception or compression filter.
        /// </summary>
        public static readonly TimeSpan DefaultWindowSize = TimeSpan.FromDays(1);

        /// <summary>
        /// Gets a flag indicating if the filter is enabled or not.
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// Gets the filter limit type.
        /// </summary>
        public TagValueFilterDeviationType LimitType { get; private set; }

        /// <summary>
        /// Gets the filter limit threshold.
        /// </summary>
        public double Limit { get; private set; }

        /// <summary>
        /// Gets the filter window size.
        /// </summary>
        public TimeSpan WindowSize { get; private set; }


        /// <summary>
        /// Creates a new <see cref="TagValueFilterSettings"/> object.
        /// </summary>
        /// <param name="isEnabled">A flag indicating if the filter is enabled or not.</param>
        /// <param name="limitType">The filter limit type.</param>
        /// <param name="limit">The filter limit threshold.</param>
        /// <param name="windowSize">The filter window size.</param>
        public TagValueFilterSettings(bool isEnabled, TagValueFilterDeviationType limitType, double limit, TimeSpan windowSize) {
            IsEnabled = isEnabled;
            LimitType = limitType;
            Limit = limit;
            WindowSize = windowSize;
        }


        /// <summary>
        /// Updates the filter settings.
        /// </summary>
        /// <param name="update">The updated settings.</param>
        /// <exception cref="ArgumentNullException"><paramref name="update"/> is <see langword="null"/>.</exception>
        public void Update(TagValueFilterSettingsUpdate update) {
            if (update == null) {
                throw new ArgumentNullException(nameof(update));
            }

            IsEnabled = update.IsEnabled;
            if (update.LimitType.HasValue) {
                LimitType = update.LimitType.Value;
            }
            if (update.Limit.HasValue) {
                Limit = update.Limit.Value;
            }
            if (update.WindowSize.HasValue) {
                WindowSize = update.WindowSize.Value;
            }
        }

    }
}
