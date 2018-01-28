using System;
using System.Collections.Generic;
using System.Text;
using Aika.Tags;

namespace Aika.DataFilters {

    /// <summary>
    /// Describes settings for a compression exception filter i.e. a filter that monitors incoming 
    /// snapshot values and, when an incoming value changes significantly enough from the current 
    /// snapshot, sends previously-received values for archiving.
    /// </summary>
    /// <remarks>
    /// Exception and compression filtering are described here: https://www.youtube.com/watch?v=89hg2mme7S0
    /// </remarks>
    public sealed class CompressionFilterState {

        /// <summary>
        /// Gets the filter settings.
        /// </summary>
        public TagValueFilterSettings Settings { get; }

        /// <summary>
        /// Gets the last-archived value for the compression filter.  This, combined with the 
        /// <see cref="LastReceivedValue"/>, defines the compression angle that incoming values are 
        /// compared with to determine if a value must be archived or not.  The compression filter 
        /// requires a last-archived value and a snapshot value to be set before it can correctly 
        /// start applying compression rules to incoming values.  If either <see cref="LastArchivedValue"/> 
        /// or <see cref="LastReceivedValue"/> are not set when the filter is initialised, the compression 
        /// filter will require multiple incoming values to be passed to it before it starts applying 
        /// the compression rules as expected.
        /// </summary>
        /// <remarks>
        /// Note that this property will be updated by the filter as incoming values are processed.
        /// </remarks>
        public TagValue LastArchivedValue { get; internal set; }

        /// <summary>
        /// Gets the last-received value for the compression filter.  This, combined with the 
        /// <see cref="LastArchivedValue"/>, defines the compression angle that incoming values 
        /// are compared with to determine if values must be archived or not.  If either 
        /// <see cref="LastArchivedValue"/> or <see cref="LastReceivedValue"/> are not set when the filter 
        /// is initialised, the compression filter will require multiple incoming values to be passed 
        /// to it before it starts applying the compression rules as expected.
        /// </summary>
        /// <remarks>
        /// Note that this property will be updated by the filter as incoming values are processed.
        /// </remarks>
        public TagValue LastReceivedValue { get; internal set; }

        /// <summary>
        /// The results of the processing that the compression filter performed on its 
        /// most-recently-received value.
        /// </summary>
        private CompressionFilterResult _lastResult;

        /// <summary>
        /// Gets the results of the processing that the compression filter performed on its 
        /// most-recently-received value.
        /// </summary>
        public CompressionFilterResult LastResult {
            get { return _lastResult; }
            set {
                _lastResult = value;
                if (_lastResult != null) {
                    ValueProcessed?.Invoke(_lastResult);
                }
            }
        }

        /// <summary>
        /// Raised once an incoming value has been processed by the compression filter.
        /// </summary>
        public event Action<CompressionFilterResult> ValueProcessed;


        /// <summary>
        /// Creates a new <see cref="CompressionFilterState"/> object.
        /// </summary>
        /// <param name="settings">The filter settings to use.</param>
        /// <param name="lastArchivedValue">
        ///   The last-archived value to initialise the compression filter with.  This value, combined 
        ///   with <paramref name="lastReceivedValue"/>, defines the initial compression angle that 
        ///   incoming values are compared with to determine if a value must be archived or not.  If 
        ///   either <paramref name="lastArchivedValue"/> or <paramref name="lastReceivedValue"/> 
        ///   are not set, the compression filter will require multiple incoming values to be passed to 
        ///   it before it starts applying the compression rules as expected.
        /// </param>
        /// <param name="lastReceivedValue">
        ///   The initial last-received value for the compression filter.  This value, combined with 
        ///   <paramref name="lastArchivedValue"/>, defines the initial compression angle that 
        ///   incoming values are compared with to determine if a value must be archived or not.  If 
        ///   either <paramref name="lastArchivedValue"/> or <paramref name="lastReceivedValue"/> 
        ///   are not set, the compression filter will require multiple incoming values to be passed to 
        ///   it before it starts applying the compression rules as expected.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="lastArchivedValue"/> and <paramref name="lastReceivedValue"/> are both defined, and <paramref name="lastArchivedValue"/> has a sample time that is greater than <paramref name="lastReceivedValue"/>.</exception>
        public CompressionFilterState(TagValueFilterSettings settings, TagValue lastArchivedValue, TagValue lastReceivedValue) {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));

            if (lastArchivedValue != null && lastReceivedValue != null && lastArchivedValue.UtcSampleTime > lastReceivedValue.UtcSampleTime) {
                throw new ArgumentException(Resources.Error_CompressionFilter_LastArchivedValueCannotBeNewerThanLastReceivedValue, nameof(lastArchivedValue));
            }

            LastArchivedValue = lastArchivedValue;
            LastReceivedValue = lastReceivedValue;
        }

    }
}
