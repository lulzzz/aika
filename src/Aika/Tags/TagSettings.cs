using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Aika.Tags {

    /// <summary>
    /// Describes settings that can be used to configure a <see cref="TagDefinition"/>.
    /// </summary>
    public class TagSettings {

        /// <summary>
        /// Gets or sets the tag name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the tag description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the tag units.
        /// </summary>
        public string Units { get; set; }

        /// <summary>
        /// Gets or sets the tag data type.
        /// </summary>
        public TagDataType DataType { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="Aika.StateSet"/> that the tag will use, if the 
        /// <see cref="DataType"/> is <see cref="TagDataType.State"/>.
        /// </summary>
        public string StateSet { get; set; }

        /// <summary>
        /// Gets or sets the exception filter settings for the tag.
        /// </summary>
        public TagValueFilterSettingsUpdate ExceptionFilterSettings { get; set; }

        /// <summary>
        /// Gets or sets the compression filter settings for the tag.
        /// </summary>
        public TagValueFilterSettingsUpdate CompressionFilterSettings { get; set; }


        /// <summary>
        /// Creates a new <see cref="TagSettings"/> object.
        /// </summary>
        public TagSettings() {}


        /// <summary>
        /// Creates a clone of the specified <see cref="TagSettings"/> object.
        /// </summary>
        /// <param name="other">The object to clone.</param>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
        internal TagSettings(TagSettings other): this() {
            if (other == null) {
                throw new ArgumentNullException(nameof(other));
            }

            Name = other.Name;
            Description = other.Description;
            Units = other.Units;
            DataType = other.DataType;
            StateSet = other.StateSet;
            ExceptionFilterSettings = other.ExceptionFilterSettings == null
                ? null
                : new TagValueFilterSettingsUpdate() {
                    IsEnabled = other.ExceptionFilterSettings.IsEnabled,
                    LimitType = other.ExceptionFilterSettings.LimitType,
                    Limit = other.ExceptionFilterSettings.Limit,
                    WindowSize = other.ExceptionFilterSettings.WindowSize
                };
            CompressionFilterSettings = other.CompressionFilterSettings == null
                ? null
                : new TagValueFilterSettingsUpdate() {
                    IsEnabled = other.CompressionFilterSettings.IsEnabled,
                    LimitType = other.CompressionFilterSettings.LimitType,
                    Limit = other.CompressionFilterSettings.Limit,
                    WindowSize = other.CompressionFilterSettings.WindowSize
                };
        }

    }
}
