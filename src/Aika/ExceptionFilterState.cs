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
        public TagValue LastExceptionValue { get; internal set; }

        /// <summary>
        /// Gets the last value that was received by the exception filter (regardless of whether or 
        /// not it passed through the filter).
        /// </summary>
        public TagValue LastReceivedValue { get; internal set; }

        /// <summary>
        /// The results of the processing that the exception filter performed on its 
        /// most-recently-received value.
        /// </summary>
        private ExceptionFilterResult _lastResult;

        /// <summary>
        /// Gets the results of the processing that the exception filter performed on its 
        /// most-recently-received value.
        /// </summary>
        public ExceptionFilterResult LastResult {
            get { return _lastResult; }
            set {
                _lastResult = value;
                if (_lastResult != null) {
                    ValueProcessed?.Invoke(_lastResult);
                }
            }
        }

        /// <summary>
        /// Raised once an incoming value has been processed by the exception filter.
        /// </summary>
        public event Action<ExceptionFilterResult> ValueProcessed;


        /// <summary>
        /// Creates a new <see cref="ExceptionFilterState"/> object.
        /// </summary>
        /// <param name="settings">The settings to use.</param>
        /// <param name="initialExceptionValue">
        ///   The initial exception value that the filter should use.  If <see langword="null"/>, the 
        ///   first value passed to the filter will always be considered to be exceptional.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
        public ExceptionFilterState(TagValueFilterSettings settings, TagValue initialExceptionValue) {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            LastExceptionValue = initialExceptionValue;
            LastReceivedValue = initialExceptionValue;
        }

    }
}
