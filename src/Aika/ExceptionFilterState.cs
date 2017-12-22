using System;
using System.Collections.Generic;
using System.Text;

namespace Aika
{
    /// <summary>
    /// Describes settings for a data exception filter i.e. a filter that discards incoming snapshot 
    /// values that are not considered to be different enough from the previous value to warrant further 
    /// action.
    /// </summary>
    /// <remarks>
    /// Exception and compression filtering are described here: https://www.youtube.com/watch?v=89hg2mme7S0
    /// </remarks>
    public sealed class ExceptionFilterState {

        /// <summary>
        /// Gets the filter settings.
        /// </summary>
        public TagValueFilterSettings Settings { get; }


        /// <summary>
        /// Gets the most recent value that passed through the exception filter.  If  <see langword="null"/>, 
        /// the next value passed to the filter will always be considered to be exceptional.
        /// </summary>
        /// <remarks>
        /// Note that this property will be updated by the filter as incoming values are processed.
        /// </remarks>
        public TagValue LastExceptionValue { get; internal set; }


        /// <summary>
        /// Creates a new <see cref="ExceptionFilterState"/> object.
        /// </summary>
        /// <param name="settings">The settings to use.</param>
        /// <param name="initialExceptionValue">
        ///   The initial exception value that the filter should use.  If <see langword="null"/>, the 
        ///   first value passed to the filter will always be considered to be exceptional.
        /// </param>
        public ExceptionFilterState(TagValueFilterSettings settings, TagValue initialExceptionValue) {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            LastExceptionValue = initialExceptionValue;
        }

    }
}
