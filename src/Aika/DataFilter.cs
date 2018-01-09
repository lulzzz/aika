using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Aika {

    /// <summary>
    /// Allows exception and/or compression settings to be applied in incoming real-time data, 
    /// so that values are filtered from the stream unless they represent a significant change.
    /// </summary>
    /// <remarks>
    /// 
    /// <para>
    /// IMPORTANT: A different instance of this class is required for each tag being monitored.
    /// </para>
    /// 
    /// <para>
    /// The <see cref="DataFilter"/> class performs exception and compression filtering on 
    /// incoming snapshot values as described here: https://www.youtube.com/watch?v=89hg2mme7S0.
    /// </para>
    /// 
    /// <para>
    /// The exception filter is used to determine if an incoming value should replace the existing 
    /// snapshot value for a tag.   To receive notifications when incoming values are processed by 
    /// the exception filter, subscribe to the <see cref="ExceptionFilterState.ValueProcessed"/> 
    /// event on the <see cref="ExceptionFilter"/> property.
    /// </para>
    /// 
    /// <para>
    /// The compression filter is used to determing when tag values should be written to a data archive 
    /// in order to maintain the correct shape for the snapshot values that have been received for a tag.  
    /// To receive notifications when incoming values are processed by the compression filter, subscribe 
    /// to the <see cref="CompressionFilterState.ValueProcessed"/> event on the 
    /// <see cref="CompressionFilter"/> property.
    /// </para>
    /// 
    /// <para>
    /// To receive notifications when the compression filter determines that values should be archived, 
    /// subscribe to the <see cref="Emit"/> event.
    /// </para>
    /// 
    /// <para>
    /// Filtering can be disabled entirely by setting the <see cref="IsEnabled"/> property on the 
    /// <see cref="DataFilter"/> to <see langword="false"/>.  Additionally, exception filtering 
    /// and compression filtering can be individually disabled in the settings on the 
    /// <see cref="ExceptionFilter"/> and <see cref="CompressionFilter"/> properties respectively.
    /// </para>
    /// 
    /// <para>
    /// When the <see cref="DataFilter"/> is disabled, all incoming values will pass both the exception 
    /// and compression filter tests.  If an individual filter is disabled, incoming values will always pass the 
    /// tests for that filter.
    /// </para>
    /// 
    /// <para>
    /// A typical use case for exception filtering is in a real-time data stream.  This allows real-time 
    /// values to be forwarded from a source to a destination only when a value change is significant.  
    /// Compression filtering would typically be used by the recipient of real-time data, to determine 
    /// which values need to be archived and which values need to be stored in a snapshot cache.
    /// </para>
    /// 
    /// </remarks>
    /// <seealso cref="CompressionFilterState"/>
    /// <seealso cref="ExceptionFilterState"/>
    public sealed class DataFilter : IDisposable {

        #region [ Fields / Properties ]

        /// <summary>
        /// Flags if the object has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger Log;

        /// <summary>
        /// Flags if <see cref="Log"/> will accept debug-level messages.
        /// </summary>
        private readonly bool CanLogDebug;

        /// <summary>
        /// Flags if <see cref="Log"/> will accept trace-level messages.
        /// </summary>
        private readonly bool CanLogTraceLog;

        /// <summary>
        /// Gets a flag that specifies if trace logging is enabled at the log level and if <see cref="EnableTraceLogging"/> is set to <see langword="true"/>.
        /// </summary>
        private bool CanLogTrace {
            get { return CanLogTraceLog && EnableTraceLogging; }
        }

        /// <summary>
        /// Gets or sets a flag that specifies if trace logging is enabled on the buffer.
        /// </summary>
        public bool EnableTraceLogging { get; set; }

        /// <summary>
        /// Monitors incoming values for exception deviations.
        /// </summary>
        private readonly ExceptionMonitor _exceptionMonitor;

        /// <summary>
        /// Monitors incoming values for compression deviations.
        /// </summary>
        private readonly CompressionMonitor _compressionMonitor;

        /// <summary>
        /// The default name to use if <see cref="Name"/> is not set.
        /// </summary>
        private readonly string _defaultName = Guid.NewGuid().ToString();

        /// <summary>
        /// The optional name for the buffer.
        /// </summary>
        private string _name;

        /// <summary>
        /// Gets or sets an optional name for the buffer (e.g. the name of the tag that the buffer is 
        /// being used for).  If set to <see langword="null"/>, a default name will be provided.
        /// </summary>
        public string Name {
            get { return String.IsNullOrWhiteSpace(_name) ? _defaultName : _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Gets or sets a flag that specifies if the <see cref="DataFilter"/> is enabled or 
        /// not.  When disabled, every incoming value will automatically pass the exception and 
        /// compression filters.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets the exception filter settings for the buffer.  The exception filter is used to determine 
        /// if the buffer's snapshot value should be updated.
        /// </summary>
        public ExceptionFilterState ExceptionFilter { get; }

        /// <summary>
        /// Gets the compression filter settings for the buffer.  The compression filter is used to determine 
        /// if a new value should be sent for archiving (via the <see cref="Emit"/> event).
        /// </summary>
        public CompressionFilterState CompressionFilter { get; }

        #endregion

        #region [ Events ]

        /// <summary>
        /// Raised when the <see cref="DataFilter"/> is disposed.
        /// </summary>
        public event Action Disposed;

        /// <summary>
        /// Raised whenever the <see cref="DataFilter"/> emits values that must be archived and/or a 
        /// change in the next archive value candidate.
        /// </summary>
        public event CompressionFilterOutputDelegate Emit;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new <see cref="DataFilter"/> object using the specified exception filter and compression filter settings.
        /// </summary>
        /// <param name="name">The name for the filter.</param>
        /// <param name="exceptionSettings">The exception filter settings.  If <see langword="null"/>, a default (disabled) exception filter will be assigned to the buffer.</param>
        /// <param name="compressionSettings">The compression filter settings.  If <see langword="null"/>, a default (disabled) compression filter will be assigned to the buffer.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging.</param>
        public DataFilter(string name, ExceptionFilterState exceptionSettings, CompressionFilterState compressionSettings, ILoggerFactory loggerFactory) {
            Log = loggerFactory?.CreateLogger<DataFilter>();
            CanLogDebug = Log?.IsEnabled(LogLevel.Debug) ?? false;
            CanLogTraceLog = Log?.IsEnabled(LogLevel.Trace) ?? false;

            Name = name;
            ExceptionFilter = exceptionSettings ?? throw new ArgumentNullException(nameof(exceptionSettings));
            CompressionFilter = compressionSettings ?? throw new ArgumentNullException(nameof(compressionSettings));
            _exceptionMonitor = new ExceptionMonitor(this);
            _compressionMonitor = new CompressionMonitor(this);
            IsEnabled = true;
        }

        #endregion

        #region [ Event Helper Methods ]

        /// <summary>
        /// Gets the value to display for a data sample in log messages etc.
        /// </summary>
        /// <param name="value">The data sample.</param>
        /// <returns>
        /// The value to display.
        /// </returns>
        private object GetSampleValueForDisplay(TagValue value) {
            return Double.IsNaN(value.NumericValue) ? (object) $"\"{value.TextValue}\"" : value.NumericValue;
        }


        /// <summary>
        /// Invokes the compression filter on the specified values.
        /// </summary>
        /// <param name="values">The values.</param>
        private void OnExceptionFilterPassed((TagValue value, string notes)[] values) {
            // Pass the values into the compression filter.
            foreach (var value in values) {
                try {
                    _compressionMonitor.ValueReceived(value);
                }
                catch (Exception e) {
                    Log?.LogError($"[{Name}] An unhandled exception occurred while invoking the compression filter.  The incoming value was: {GetSampleValueForDisplay(value.value)} @ {value.value.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fffZ}", e);
                }
            }
        }


        /// <summary>
        /// Invokes the <see cref="Emit"/> event using the provided values.
        /// </summary>
        /// <param name="valuesToArchive">The values to send on for archiving.</param>
        /// <param name="nextArchiveCandidate">The updated next-archive candidate.</param>
        private void OnCompressionFilterEmit(IEnumerable<TagValue> valuesToArchive, TagValue nextArchiveCandidate) {
            var vals = valuesToArchive?.ToArray() ?? new TagValue[0];

            try {
                if (Emit != null) {
                    if (vals.Length > 0 && CanLogDebug) {
                        Log?.LogDebug($"[{Name}] New values will be sent to subscribers for archiving: [ {String.Join(", ", vals.Select(x => $"{GetSampleValueForDisplay(x)} @ {x.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fffZ}"))} ]");
                    }
                    Emit.Invoke(vals, nextArchiveCandidate);
                }
            }
            catch (Exception e) {
                Log?.LogError($"[{Name}] An unhandled exception occurred while invoking {nameof(Emit)} event handlers.", e);
            }
        }

        #endregion

        #region [ Public Methods ]

        /// <summary>
        /// Informs the <see cref="DataFilter"/> that a new snapshot value has been received for the tag.
        /// </summary>
        /// <param name="incoming">The incoming tag value.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="DataFilter"/> has been disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="incoming"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// 
        /// <para>
        /// Incoming values that pass the exception filter will then be passed to the compression filter.  
        /// If the values pass the compression filter, the <see cref="Emit"/> event will be raised, 
        /// allowing subscribers to be notified about values that should be written to a data archive.
        /// </para>
        /// 
        /// </remarks>
        public void ValueReceived(TagValue incoming) {
            if (_isDisposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (incoming == null) {
                throw new ArgumentNullException(nameof(incoming));
            }

            _exceptionMonitor.ValueReceived((incoming, null));
        }


        /// <summary>
        /// Disposes of the <see cref="DataFilter"/>, preventing if from receiving new values.
        /// </summary>
        public void Dispose() {
            if (_isDisposed) {
                return;
            }

            _isDisposed = true;
            Disposed?.Invoke();
        }

        #endregion

        #region [ Inner Types ]

        /// <summary>
        /// Monitors incoming snapshot values and decides if those values should be emitted for further processing e.g. by a compression monitor.
        /// </summary>
        private class ExceptionMonitor {

            /// <summary>
            /// Gets the compression filter settings.
            /// </summary>
            public DataFilter Filter { get; }

            /// <summary>
            /// Gets the value that passed the exception filter test prior to the most-recent exception value.
            /// </summary>
            private TagValue _previousExceptionValue;

            
            /// <summary>
            /// Creates a new <see cref="ExceptionMonitor"/> object.
            /// </summary>
            /// <param name="filter">The data compression buffer.</param>
            /// <exception cref="ArgumentNullException"><paramref name="filter"/> is <see langword="null"/>.</exception>
            public ExceptionMonitor(DataFilter filter) {
                Filter = filter ?? throw new ArgumentNullException(nameof(filter));
            }


            /// <summary>
            /// Tests if an incoming snapshot tag value passes (i.e. exceeds) the buffer's exception filter.
            /// </summary>
            /// <param name="incoming">The incoming tag value.</param>
            /// <param name="reason">The reason for the pass/fail (for debugging purposes only).</param>
            /// <param name="limits">The limits that were calculated.</param>
            /// <returns>
            /// <see langword="true"/> if <paramref name="incoming"/> is considered to be exceptional, otherwise <see langword="false"/>.
            /// </returns>
            private bool PassesExceptionFilter(TagValue incoming, out string reason, out CalculatedLimits limits) {
                if (Filter.ExceptionFilter.LastExceptionValue == null) {
                    reason = "A last exception value has not been set on the exception monitor.";
                    limits = null;
                    return true;
                }

                // Never allow values older than the current snapshot.
                if (Filter.ExceptionFilter.LastExceptionValue != null && incoming.UtcSampleTime < Filter.ExceptionFilter.LastExceptionValue.UtcSampleTime) {
                    reason = "The incoming value is earlier than the current exception value.";
                    limits = null;
                    return false;
                }

                if (Filter.ExceptionFilter.LastReceivedValue == null) {
                    reason = "A last-received value has not been set on the exception monitor.";
                    limits = null;
                    return true;
                }

                if (!Filter.IsEnabled) {
                    reason = "The data filter is disabled.";
                    limits = null;
                    return true;
                }

                if (!Filter.ExceptionFilter.Settings.IsEnabled) {
                    reason = "Exception filtering is disabled.";
                    limits = null;
                    return true;
                }

                if ((incoming.UtcSampleTime - Filter.ExceptionFilter.LastExceptionValue.UtcSampleTime) > Filter.ExceptionFilter.Settings.WindowSize) {
                    reason = $"The current exception value has a time stamp ({Filter.ExceptionFilter.LastExceptionValue.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fff}Z) that is more than {Filter.ExceptionFilter.Settings.WindowSize} older than the incoming value ({incoming.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fff}Z).";
                    limits = null;
                    return true;
                }

                if (incoming.Quality != Filter.ExceptionFilter.LastExceptionValue.Quality) {
                    reason = $"The incoming value has a quality status ({incoming.Quality}) that is different to the current exception value ({Filter.ExceptionFilter.LastExceptionValue.Quality})";
                    limits = null;
                    return true;
                }

                if (Double.IsNaN(incoming.NumericValue) && !String.Equals(incoming.TextValue, Filter.ExceptionFilter.LastExceptionValue.TextValue)) {
                    reason = $"The incoming value is non-numeric and has a different text value (\"{incoming.TextValue}\") to the current exception value ({Filter.ExceptionFilter.LastExceptionValue.TextValue}).";
                    limits = null;
                    return true;
                }

                if (!Double.IsNaN(incoming.NumericValue) && Double.IsNaN(Filter.ExceptionFilter.LastExceptionValue.NumericValue)) {
                    reason = $"The incoming  value is numeric ({incoming.NumericValue}), whereas the current exception value is non-numeric (\"{Filter.ExceptionFilter.LastExceptionValue.TextValue}\").";
                    limits = null;
                    return true;
                }

                double exceptionDeviation = 0;

                if (Filter.ExceptionFilter.Settings.Limit != 0) {
                    switch (Filter.ExceptionFilter.Settings.LimitType) {
                        case TagValueFilterDeviationType.Fraction:
                            exceptionDeviation = Math.Abs(Filter.ExceptionFilter.LastExceptionValue.NumericValue) * Filter.ExceptionFilter.Settings.Limit;
                            break;
                        case TagValueFilterDeviationType.Percentage:
                            exceptionDeviation = Math.Abs(Filter.ExceptionFilter.LastExceptionValue.NumericValue) * (Filter.ExceptionFilter.Settings.Limit / 100);
                            break;
                        default:
                            // Absolute limit type.
                            exceptionDeviation = Filter.ExceptionFilter.Settings.Limit;
                            break;
                    }
                }

                var maxValue = Filter.ExceptionFilter.LastExceptionValue.NumericValue + exceptionDeviation;
                var minValue = Filter.ExceptionFilter.LastExceptionValue.NumericValue - exceptionDeviation;

                if (minValue > maxValue) {
                    var tmp = maxValue;
                    maxValue = minValue;
                    minValue = tmp;
                }

                limits = new CalculatedLimits(minValue, maxValue);

                if (incoming.NumericValue < minValue) {
                    reason = $"The incoming value ({incoming.NumericValue}) is less than the minimum exception deviation limit ({minValue}).";
                    return true;
                }

                if (incoming.NumericValue > maxValue) {
                    reason = $"The incoming value ({incoming.NumericValue}) is greater than the maximum exception deviation limit ({maxValue}).";
                    return true;
                }

                reason = "The incoming value is inside the exception deviation limits.";
                return false;
            }


            /// <summary>
            /// Informs the <see cref="ExceptionMonitor"/> that a new value has been received from the tag's source.
            /// </summary>
            /// <param name="incoming">The incoming value.</param>
            /// <returns>
            /// A flag that indicates if the incoming value passed the exception filter.
            /// </returns>
            public bool ValueReceived((TagValue value, string notes) incoming) {
                if (incoming.value == null) {
                    return false;
                }

                var now = DateTime.UtcNow;

                ExceptionFilterResult result = null;

                try {
                    if (!PassesExceptionFilter(incoming.value, out var reason, out var limits)) {
                        if (Filter.CanLogTrace) {
                            Filter.Log.LogTrace($"[{Filter.Name}] The exception filter rejected the incoming value for the following reason: {reason}");
                        }

                        result = new ExceptionFilterResult(now, incoming.value, incoming.notes, new ExceptionFilterResultDetails(true, reason, Filter.ExceptionFilter.LastExceptionValue, Filter.ExceptionFilter.Settings, limits == null ? null : new ExceptionFilterResultDetails.ExceptionLimits(limits.MinimumLimit, limits.MaximumLimit)));

                        Filter.ExceptionFilter.LastReceivedValue = incoming.value;
                        return false;
                    }
                    else {
                        if (Filter.CanLogTrace) {
                            Filter.Log.LogTrace($"[{Filter.Name}] The incoming value passed the exception filter test.  The reason given was: {reason}");
                        }

                        result = new ExceptionFilterResult(now, incoming.value, null, new ExceptionFilterResultDetails(false, reason, Filter.ExceptionFilter.LastExceptionValue, Filter.ExceptionFilter.Settings, limits == null ? null : new ExceptionFilterResultDetails.ExceptionLimits(limits.MinimumLimit, limits.MaximumLimit)));

                        _previousExceptionValue = Filter.ExceptionFilter.LastExceptionValue;
                        Filter.ExceptionFilter.LastExceptionValue = incoming.value;

                        var lastReceived = Filter.ExceptionFilter.LastReceivedValue;

                        // The lastReceived != LastSnapshotValue test below is used to ensure that, if we 
                        // are reporting a new snapshot value, we won't send the last-received value if 
                        // the last-received value was actually the previous value that passed the exception 
                        // filter, since this value has already been sent previously.
                        var snapshotValues = lastReceived != null && lastReceived.NumericValue != incoming.value.NumericValue && lastReceived != _previousExceptionValue
                            ? new(TagValue value, string notes)[] {
                                (lastReceived, "This is the value received immediately before the value that passed the exception filter."),
                                incoming
                            }
                            : new(TagValue value, string notes)[] {
                                incoming
                            };

                        Filter.OnExceptionFilterPassed(snapshotValues);
                        Filter.ExceptionFilter.LastReceivedValue = incoming.value;

                        return true;
                    }


                }
                finally {
                    if (result != null) {
                        Filter.ExceptionFilter.LastResult = result;
                    }
                }
            }
        }


        /// <summary>
        /// Class that monitors incoming snapshot values to check if the value should be sent for archiving.
        /// </summary>
        private class CompressionMonitor {

            /// <summary>
            /// Gets the data buffer.
            /// </summary>
            public DataFilter Filter { get; }

            /// <summary>
            /// Gets the maximum compression deviation limit to use in the compression filter for the <see cref="DataFilter"/>.
            /// </summary>
            public double CompressionMaximum { get; private set; }

            /// <summary>
            /// Gets the minimum compression deviation limit to use in the compression filter for the <see cref="DataFilter"/>.
            /// </summary>
            public double CompressionMinimum { get; private set; }


            /// <summary>
            /// Creates a new <see cref="CompressionMonitor"/> object.
            /// </summary>
            /// <param name="filter">The data compression buffer.</param>
            /// <exception cref="ArgumentNullException"><paramref name="filter"/> is <see langword="null"/>.</exception>
            public CompressionMonitor(DataFilter filter) {
                Filter = filter ?? throw new ArgumentNullException(nameof(filter));
            }


            /// <summary>
            /// Calculates the compression limits for a tag value, based on the numeric value of the sample 
            /// and the compression filter settings.  Note that this does not take existing compression 
            /// limits on the current snapshot into account; this comparison must be done elsewhere if 
            /// required.
            /// </summary>
            /// <param name="value">The value to calculate the compression limits for.</param>
            /// <returns>The new compression limits</returns>
            private CalculatedLimits CalculateNewCompressionLimits(TagValue value) {
                // If the value is non-numeric, set the compression limits so that the maximum limit is 
                // Double.MinValue and the maximum limit is Double.MaxValue i.e. the next incoming 
                // numerical value will pass the filter.
                if (Double.IsNaN(value.NumericValue)) {
                    return new CalculatedLimits(Double.MinValue, Double.MaxValue);
                }

                double compressionDeviation = 0;

                switch (Filter.CompressionFilter.Settings.LimitType) {
                    case TagValueFilterDeviationType.Fraction:
                        compressionDeviation = Math.Abs(value.NumericValue) * Filter.CompressionFilter.Settings.Limit;
                        break;
                    case TagValueFilterDeviationType.Percentage:
                        compressionDeviation = Math.Abs(value.NumericValue) * (Filter.CompressionFilter.Settings.Limit / 100);
                        break;
                    default:
                        // Absolute limit type.
                        compressionDeviation = Filter.CompressionFilter.Settings.Limit;
                        break;
                }

                var maxCompressionValue = value.NumericValue + compressionDeviation;
                var minCompressionValue = value.NumericValue - compressionDeviation;

                if (minCompressionValue > maxCompressionValue) {
                    var tmp = maxCompressionValue;
                    maxCompressionValue = minCompressionValue;
                    minCompressionValue = tmp;
                }

                return new CalculatedLimits(minCompressionValue, maxCompressionValue);
            }


            /// <summary>
            /// Interpolates the compression limits to test an incoming value against, using the max/min 
            /// slopes calculated using the last-archived value for the tag, and the last-received 
            /// value for the tag.
            /// </summary>
            /// <param name="incoming">The incoming snapshot tag value.</param>
            /// <param name="lastArchived">The last value that was sent for archiving.</param>
            /// <param name="lastReceived">The most-recently received snapshot tag value prior to the incoming value.</param>
            /// <returns>A tuple where the first value is the *MAXIMUM* limit and the second value is the *MINIMUM* limit.</returns>
            private CalculatedLimits InterpolateCompressionLimitsFromSlope(TagValue incoming, TagValue lastArchived, TagValue lastReceived) {
                var x0 = Filter.CompressionFilter.LastArchivedValue.UtcSampleTime.Ticks;
                var x1 = Filter.CompressionFilter.LastReceivedValue.UtcSampleTime.Ticks;
                var x = incoming.UtcSampleTime.Ticks;

                var y0 = Filter.CompressionFilter.LastArchivedValue.NumericValue;
                var y1_max = CompressionMaximum;
                var y1_min = CompressionMinimum;

                var y_max = y0 + (x - x0) * ((y1_max - y0) / (x1 - x0));
                var y_min = y0 + (x - x0) * ((y1_min - y0) / (x1 - x0));

                return new CalculatedLimits(y_min, y_max);
            }


            /// <summary>
            /// Tests if an incoming snapshot tag value passes (i.e. exceeds) the buffer's compression filter.
            /// </summary>
            /// <param name="incoming">The incoming value.</param>
            /// <param name="reason">The reason for the pass/fail.</param>
            /// <param name="interpolatedCompressionLimits">The interpolated compression limits that the <paramref name="incoming"/> value was tested against.</param>
            /// <returns>
            /// <see langword="true"/> if <paramref name="incoming"/> is considered to be exceptional, otherwise <see langword="false"/>.
            /// </returns>
            private bool PassesCompressionFilter(TagValue incoming, out string reason, out CalculatedLimits interpolatedCompressionLimits) {
                if (!Filter.IsEnabled) {
                    reason = "The data filter is disabled.";
                    interpolatedCompressionLimits = null;
                    return true;
                }

                if (!Filter.CompressionFilter.Settings.IsEnabled) {
                    reason = "Compression filtering is disabled.";
                    interpolatedCompressionLimits = null;
                    return true;
                }

                if (Filter.CompressionFilter.LastReceivedValue == null) {
                    reason = "The compression buffer does not contain a last-received value.";
                    interpolatedCompressionLimits = null;
                    return true;
                }

                if (Filter.CompressionFilter.LastArchivedValue == null) {
                    reason = "The compression buffer does not contain a last-archived value.";
                    interpolatedCompressionLimits = null;
                    return true;
                }

                if ((incoming.UtcSampleTime - Filter.CompressionFilter.LastArchivedValue.UtcSampleTime) > Filter.CompressionFilter.Settings.WindowSize) {
                    reason = $"The last-archived value has a time stamp ({Filter.CompressionFilter.LastArchivedValue.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fff}Z) that is more than {Filter.CompressionFilter.Settings.WindowSize} older than the incoming value ({incoming.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fff}Z).";
                    interpolatedCompressionLimits = null;
                    return true;
                }

                if (incoming.Quality != Filter.CompressionFilter.LastReceivedValue.Quality) {
                    reason = $"The incoming value has a quality status ({incoming.Quality}) that is different to the last-received value ({Filter.CompressionFilter.LastReceivedValue.Quality}).";
                    interpolatedCompressionLimits = null;
                    return true;
                }

                if (Double.IsNaN(incoming.NumericValue) && !String.Equals(incoming.TextValue, Filter.CompressionFilter.LastReceivedValue.TextValue)) {
                    reason = $"The incoming value is non-numeric and has a different text value (\"{incoming.TextValue}\") to the last-received value ({Filter.CompressionFilter.LastReceivedValue.TextValue}).";
                    interpolatedCompressionLimits = null;
                    return true;
                }

                if (!Double.IsNaN(incoming.NumericValue) && Double.IsNaN(Filter.CompressionFilter.LastReceivedValue.NumericValue)) {
                    reason = $"The incoming value is numeric ({incoming.NumericValue}), whereas the last-received value is non-numeric (\"{Filter.CompressionFilter.LastReceivedValue.TextValue}\").";
                    interpolatedCompressionLimits = null;
                    return true;
                }

                if (Double.IsNaN(CompressionMaximum)) {
                    reason = $"The current maximum compression slope value is {Double.NaN}.";
                    interpolatedCompressionLimits = null;
                    return true;
                }

                if (Double.IsNaN(CompressionMinimum)) {
                    reason = $"The current minimum compression slope value is {Double.NaN}.";
                    interpolatedCompressionLimits = null;
                    return true;
                }

                interpolatedCompressionLimits = InterpolateCompressionLimitsFromSlope(incoming, Filter.CompressionFilter.LastArchivedValue, Filter.CompressionFilter.LastReceivedValue);
                var y_max = interpolatedCompressionLimits.MaximumLimit;
                var y_min = interpolatedCompressionLimits.MinimumLimit;

                if (incoming.NumericValue > y_max) {
                    reason = $"The incoming value ({incoming.NumericValue} @ {incoming.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fff}Z) is greater than the compression deviation limit calculated for the incoming time stamp ({y_max}).";
                    return true;
                }

                if (incoming.NumericValue < y_min) {
                    reason = $"The incoming value ({incoming.NumericValue} @ {incoming.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fff}Z) is less than the compression deviation limit calculated for the incoming sample time stamp ({y_min}).";
                    return true;
                }

                reason = $"The incoming value ({incoming.NumericValue} @ {incoming.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fff}Z) is inside the compression deviation limits calculated for the incoming sample time stamp ({y_min} <= x <= {y_max}).";
                return false;
            }


            /// <summary>
            /// Informs the <see cref="CompressionMonitor"/> that a new snapshot value has been received.
            /// </summary>
            /// <param name="incoming">The incoming value.</param>
            /// <returns>A flag that indicates if the incoming value passed the compression filter.</returns>
            public bool ValueReceived((TagValue value, string notes) incoming) {
                if (incoming.value == null) {
                    return false;
                }

                var now = DateTime.UtcNow;

                CompressionFilterResult result = null;

                try {
                    string reason;
                    CalculatedLimits interpolatedCompressionLimits;

                    var passesCompression = PassesCompressionFilter(incoming.value, out reason, out interpolatedCompressionLimits);
                    var calculatedCompressionLimits = CalculateNewCompressionLimits(incoming.value);

                    if (passesCompression) {
                        if (Filter.CanLogTrace) {
                            Filter.Log.LogTrace($"[{Filter.Name}] The incoming value passed the compression filter test.  The reason given was: {reason}");
                        }

                        result = new CompressionFilterResult(
                            now,
                            incoming.value,
                            incoming.notes,
                            new CompressionFilterResultDetails(
                                !passesCompression,
                                reason,
                                Filter.CompressionFilter.LastArchivedValue,
                                Filter.CompressionFilter.LastReceivedValue,
                                Filter.CompressionFilter.Settings,
                                new CompressionFilterResultDetails.CompressionLimits(
                                    Filter.CompressionFilter.LastArchivedValue == null
                                        ? null
                                        : new CompressionFilterResultDetails.CompressionLimitSet(Filter.CompressionFilter.LastArchivedValue.UtcSampleTime, CompressionMinimum, CompressionMaximum),
                                    interpolatedCompressionLimits == null
                                        ? null
                                        : new CompressionFilterResultDetails.CompressionLimitSet(incoming.value.UtcSampleTime, interpolatedCompressionLimits.MinimumLimit, interpolatedCompressionLimits.MaximumLimit),
                                    new CompressionFilterResultDetails.CompressionLimitSet(incoming.value.UtcSampleTime, calculatedCompressionLimits.MinimumLimit, calculatedCompressionLimits.MaximumLimit)
                                )
                            )
                        );

                        var valueToArchive = Filter.CompressionFilter.LastReceivedValue;

                        if (valueToArchive != null) {
                            Filter.CompressionFilter.LastArchivedValue = valueToArchive;
                        }
                        else {
                            if (Filter.CanLogDebug) {
                                Filter.Log.LogDebug($"[{Filter.Name}] The incoming value passed the compression filter test, but the compression filter does not yet define a last-received value.  Therefore, no value will be sent for archiving.  This behaviour can occur the first time a value passes the compression filter after the {nameof(DataFilter)} starts up.  The incoming value will be sent for archiving the next time an incoming value passes the compression filter test.");
                            }
                        }

                        if (Filter.CanLogTrace) {
                            Filter.Log.LogTrace($"[{Filter.Name}] Updating the compression buffer's last-received value: {Filter.GetSampleValueForDisplay(incoming.value)} @ {incoming.value.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fffZ}.");
                            Filter.Log.LogTrace($"[{Filter.Name}] Adjusting compression slopes for next incoming value: Last Archived Value = {Filter.GetSampleValueForDisplay(Filter.CompressionFilter.LastArchivedValue)} @ {Filter.CompressionFilter.LastArchivedValue.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fff}Z , Compression Slope Min/Max = {calculatedCompressionLimits.MinimumLimit}/{calculatedCompressionLimits.MaximumLimit} @ {incoming.value.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fff}Z");
                        }

                        Filter.CompressionFilter.LastReceivedValue = incoming.value;
                        CompressionMaximum = calculatedCompressionLimits.MaximumLimit;
                        CompressionMinimum = calculatedCompressionLimits.MinimumLimit;

                        if (valueToArchive != null) {
                            Filter.OnCompressionFilterEmit(new[] { valueToArchive }, incoming.value);
                        }
                        else {
                            Filter.OnCompressionFilterEmit(null, incoming.value);
                        }

                        return true;
                    }
                    else {
                        if (Filter.CanLogTrace) {
                            Filter.Log.LogTrace($"[{Filter.Name}] The compression filter rejected the incoming value for the following reason: {reason}");
                        }

                        var maxCompressionValueAtNewSnapshot = calculatedCompressionLimits.MaximumLimit;
                        if (interpolatedCompressionLimits != null && maxCompressionValueAtNewSnapshot > interpolatedCompressionLimits.MaximumLimit) {
                            maxCompressionValueAtNewSnapshot = interpolatedCompressionLimits.MaximumLimit;
                        }

                        var minCompressionValueAtNewSnapshot = calculatedCompressionLimits.MinimumLimit;
                        if (interpolatedCompressionLimits != null && minCompressionValueAtNewSnapshot < interpolatedCompressionLimits.MinimumLimit) {
                            minCompressionValueAtNewSnapshot = interpolatedCompressionLimits.MinimumLimit;
                        }

                        if (Filter.CanLogTrace) {
                            Filter.Log.LogTrace($"[{Filter.Name}] Updating the compression buffer's last-received value: {Filter.GetSampleValueForDisplay(incoming.value)} @ {incoming.value.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fffZ}");
                            Filter.Log.LogTrace($"[{Filter.Name}] Adjusting compression slopes for next incoming value: Last Archived Value = {Filter.GetSampleValueForDisplay(Filter.CompressionFilter.LastArchivedValue)} @ {Filter.CompressionFilter.LastArchivedValue.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fff}Z , Compression Slope Min/Max = {minCompressionValueAtNewSnapshot}/{maxCompressionValueAtNewSnapshot} @ {incoming.value.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fff}Z");
                        }

                        result = new CompressionFilterResult(
                            now,
                            incoming.value,
                            incoming.notes,
                            new CompressionFilterResultDetails(
                                !passesCompression,
                                reason,
                                Filter.CompressionFilter.LastArchivedValue,
                                Filter.CompressionFilter.LastReceivedValue,
                                Filter.CompressionFilter.Settings,
                                new CompressionFilterResultDetails.CompressionLimits(
                                    Filter.CompressionFilter.LastArchivedValue == null
                                        ? null
                                        : new CompressionFilterResultDetails.CompressionLimitSet(Filter.CompressionFilter.LastArchivedValue.UtcSampleTime, CompressionMinimum, CompressionMaximum),
                                    interpolatedCompressionLimits == null
                                        ? null
                                        : new CompressionFilterResultDetails.CompressionLimitSet(incoming.value.UtcSampleTime, interpolatedCompressionLimits.MinimumLimit, interpolatedCompressionLimits.MaximumLimit),
                                    new CompressionFilterResultDetails.CompressionLimitSet(incoming.value.UtcSampleTime, minCompressionValueAtNewSnapshot, maxCompressionValueAtNewSnapshot)
                                )
                            )
                        );

                        Filter.CompressionFilter.LastReceivedValue = incoming.value;
                        CompressionMaximum = maxCompressionValueAtNewSnapshot;
                        CompressionMinimum = minCompressionValueAtNewSnapshot;

                        Filter.OnCompressionFilterEmit(null, incoming.value);

                        return false;
                    }
                }
                finally {
                    if (result != null) {
                        Filter.CompressionFilter.LastResult = result;
                    }
                }
            }

        }


        /// <summary>
        /// Describes calculated limits used for exception or compression filtering.
        /// </summary>
        private class CalculatedLimits {

            /// <summary>
            /// Gets the minimum limit value.
            /// </summary>
            public double MinimumLimit { get; }

            /// <summary>
            /// Gets the maximum limit value.
            /// </summary>
            public double MaximumLimit { get; }


            /// <summary>
            /// Creates a new <see cref="CalculatedLimits"/> object.
            /// </summary>
            /// <param name="minimumLimit">The minimum limit.</param>
            /// <param name="maximumLimit">The maximum limit.</param>
            public CalculatedLimits(double minimumLimit, double maximumLimit) {
                MinimumLimit = minimumLimit;
                MaximumLimit = maximumLimit;
            }

        }

        #endregion

    }

}
