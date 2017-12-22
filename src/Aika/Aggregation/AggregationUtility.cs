using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Aika.Aggregation {
    /// <summary>
    /// Performs aggregation on raw tag data.
    /// </summary>
    public class AggregationUtility {

        #region [ Fields / Properties ]

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger _log;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new <see cref="AggregationUtility"/> object.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging.</param>
        public AggregationUtility(ILoggerFactory loggerFactory) {
            _log = loggerFactory?.CreateLogger<AggregationUtility>();
        }

        #endregion

        #region [ Aggregation Helpers ]

        /// <summary>
        /// Calculates the sample interval to use for the specified start time, end time, and point count.
        /// </summary>
        /// <param name="utcStartTime">The UTC start time.</param>
        /// <param name="utcEndTime">The UTC end time.</param>
        /// <param name="pointCount">The desired number of points between the start time and the end time.</param>
        /// <returns>
        /// The time span to use.
        /// </returns>
        public TimeSpan GetSampleInterval(DateTime utcStartTime, DateTime utcEndTime, int pointCount) {
            if (utcStartTime == utcEndTime) {
                return TimeSpan.FromSeconds(1);
            }

            if (pointCount < 1) {
                pointCount = 1;
            }

            if (utcStartTime > utcEndTime) {
                var tmp = utcStartTime;
                utcStartTime = utcEndTime;
                utcEndTime = tmp;
            }

            return TimeSpan.FromMilliseconds((utcEndTime - utcStartTime).TotalMilliseconds / pointCount);
        }


        /// <summary>
        /// Checks to see if the UTC start and end time for an aggregation request lie within the boundaries of the specified raw data samples.
        /// </summary>
        /// <param name="dataFunction">The data function for the aggregation.</param>
        /// <param name="rawSamples">The raw data samples.</param>
        /// <param name="utcStartTime">The UTC start time for the data query.</param>
        /// <param name="utcEndTime">The UTC end time for the data query.</param>
        private void CheckRawDataTimeRange(string dataFunction, IEnumerable<TagValue> rawSamples, DateTime utcStartTime, DateTime utcEndTime) {
            var first = rawSamples.First();
            var last = rawSamples.Last();

            if (first.UtcSampleTime > utcStartTime) {
                _log?.LogWarning($"[{dataFunction}] Query start time ({utcStartTime:yyyy-MM-ddTHH:mm:ss.fffZ}) is earlier than the earliest raw value provided for aggregation ({first.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fffZ}).");
            }
            if (last.UtcSampleTime < utcEndTime) {
                _log?.LogWarning($"[{dataFunction}] Query end time ({utcEndTime:yyyy-MM-ddTHH:mm:ss.fffZ}) is later than the latest raw value provided for aggregation ({last.UtcSampleTime:yyyy-MM-ddTHH:mm:ss.fffZ}).");
            }
        }


        /// <summary>
        /// Tests if data for a tag can be aggregated or interpolated.
        /// </summary>
        /// <param name="tag">The tag definition.</param>
        /// <returns>
        /// A <see cref="Boolean"/> that indicates if the tag's data can be aggregated or interpolated.  
        /// When <see langword="false"/>, data should be processed using one of the <see cref="Interval"/> 
        /// overloads instead.
        /// </returns>
        private bool CanAggregate(TagDefinition tag) {
            return tag.DataType != TagDataType.State && tag.DataType != TagDataType.Text;
        }


        /// <summary>
        /// Aggregates data.
        /// </summary>
        /// <param name="tag">The tag definition for the data being aggregated.</param>
        /// <param name="utcStartTime">The UTC end time for the aggregated data set.</param>
        /// <param name="utcEndTime">The UTC end time for the aggregated data set.</param>
        /// <param name="sampleInterval">The sample interval to use between aggregation calculations.</param>
        /// <param name="rawData">The raw data to be aggregated.</param>
        /// <param name="aggregateName">The aggregate name (for logging purposes only).</param>
        /// <param name="aggregateFunc">The aggregate function to use.</param>
        /// <returns>
        /// A collection of aggregated values.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="tag"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="utcStartTime"/> is greater than or equal to <paramref name="utcEndTime"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="sampleInterval"/> is less than or equal to <see cref="TimeSpan.Zero"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="rawData"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="aggregateFunc"/> is <see langword="null"/></exception>
        private IEnumerable<TagValue> GetAggregatedData(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, IEnumerable<TagValue> rawData, string aggregateName, Func<TagDefinition, DateTime, IEnumerable<TagValue>, TagValue> aggregateFunc) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (utcStartTime >= utcEndTime) {
                throw new ArgumentException("Start date cannot be greater than end date.", nameof(utcStartTime));
            }
            if (sampleInterval <= TimeSpan.Zero) {
                throw new ArgumentException("Sample interval cannot be less than or equal to zero.", nameof(sampleInterval));
            }
            if (rawData == null) {
                throw new ArgumentNullException(nameof(rawData));
            }
            if (aggregateFunc == null) {
                throw new ArgumentNullException(nameof(aggregateFunc));
            }
            if (String.IsNullOrWhiteSpace(aggregateName)) {
                aggregateName = "UNKNOWN";
            }

            // Ensure that we are only working with non-null samples.
            var rawSamples = rawData.Where(x => x != null).ToArray();
            if (rawSamples.Length == 0) {
                return new TagValue[0];
            }

            if (_log?.IsEnabled(LogLevel.Debug) ?? false) {
                _log.LogDebug($"[{aggregateName}] Performing data aggregation: Start Time = {utcStartTime:yyyy-MM-ddTHH:mm:ss.fffZ}, End Time = {utcEndTime:yyyy-MM-ddTHH:mm:ss.fffZ}, Sample Interval = {sampleInterval}, Raw Data Sample Count = {rawSamples.Length}");
            }
            CheckRawDataTimeRange(aggregateName, rawSamples, utcStartTime, utcEndTime);

            // Set the initial list capacity based on the time range and sample interval.
            var capacity = (int) ((utcEndTime - utcStartTime).TotalMilliseconds / sampleInterval.TotalMilliseconds);
            var result = capacity > 0
                ? new List<TagValue>(capacity)
                : new List<TagValue>();

            // We'll use an aggregation bucket to keep track of the time period that we are calculating 
            // the next sample over, and the samples that will be used in the aggregation.
            var bucket = new AggregationBucket() {
                UtcStart = utcStartTime.Subtract(sampleInterval),
                UtcEnd = utcStartTime
            };

            // If the initial bucket covers a period of time that starts before the raw data set that 
            // we have been given, move the start time of the bucket forward to match the first raw 
            // sample.
            var firstSample = rawSamples[0];

            if (bucket.UtcStart < firstSample.UtcSampleTime) {
                bucket.UtcStart = firstSample.UtcSampleTime;
                // Make sure that the end time of the bucket is at least equal to the start time of the bucket.
                if (bucket.UtcEnd < bucket.UtcStart) {
                    bucket.UtcEnd = bucket.UtcStart;
                }
            }

            TagValue previousAggregatedValue = null;

            var sampleEnumerator = rawSamples.AsEnumerable().GetEnumerator();
            while (sampleEnumerator.MoveNext()) {
                var currentSample = sampleEnumerator.Current;

                // If we've moved past the requested end time, break from the loop.
                if (currentSample.UtcSampleTime > utcEndTime) {
                    break;
                }

                // If we've moved past the end of the bucket, calculate the aggregate for the bucket, 
                // move to the next bucket, and repeat this process until the end time for the bucket 
                // is greater than the time stamp for currentSample.
                //
                // This allows us to handle situations where we need to produce an aggregated value at 
                // a set interval, but there is a gap in raw data that is bigger than the required 
                // interval (e.g. if we are averaging over a 5 minute interval, but there is a gap of 
                // 30 minutes between raw samples).
                while (currentSample.UtcSampleTime >= bucket.UtcEnd) {
                    if (bucket.Samples.Count > 0) {
                        // There are samples in the bucket; calculate the aggregate value.
                        var val = aggregateFunc(tag, bucket.UtcEnd, bucket.Samples);
                        result.Add(val);
                        previousAggregatedValue = val;
                        bucket.Samples.Clear();
                    }
                    else if (previousAggregatedValue != null) {
                        // There are no samples in the current bucket, but we have a value from the 
                        // previous bucket that we can re-use.
                        var val = new TagValue(bucket.UtcEnd, previousAggregatedValue.NumericValue, previousAggregatedValue.TextValue, previousAggregatedValue.Quality, tag.Units);
                        result.Add(val);
                        previousAggregatedValue = val;
                    }

                    // Set the start/end time for the next bucket.
                    bucket.UtcStart = bucket.UtcEnd;
                    bucket.UtcEnd = bucket.UtcStart.Add(sampleInterval);
                }

                bucket.Samples.Add(currentSample);
            }

            // We have moved past utcEndTime in the raw data by this point.  If we have samples in the 
            // bucket, and we either haven't calculated a value yet, or the most recent value that we 
            // calculated has a time stamp less than utcEndTime, calculate a final sample for 
            // utcEndTime and add it to the result.
            if (bucket.Samples.Count > 0 && (result.Count == 0 || (result.Count > 0 && result.Last().UtcSampleTime < utcEndTime))) {
                var val = aggregateFunc(tag, utcEndTime, bucket.Samples);
                result.Add(val);
            }

            return result;
        }


        /// <summary>
        /// Aggregates data.
        /// </summary>
        /// <param name="tag">The tag definition for the data being aggregated.</param>
        /// <param name="utcStartTime">The UTC end time for the aggregated data set.</param>
        /// <param name="utcEndTime">The UTC end time for the aggregated data set.</param>
        /// <param name="pointCount">The point count to use when calculating the interval between aggregation calculations.</param>
        /// <param name="rawData">The raw data to be aggregated.</param>
        /// <param name="aggregateName">The aggregate name (for logging purposes only).</param>
        /// <param name="aggregateFunc">The aggregate function to use.</param>
        /// <returns>
        /// A collection of aggregated values.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="tag"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="utcStartTime"/> is greater than or equal to <paramref name="utcEndTime"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="rawData"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="aggregateFunc"/> is <see langword="null"/></exception>
        private IEnumerable<TagValue> GetAggregatedData(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, int pointCount, IEnumerable<TagValue> rawData, string aggregateName, Func<TagDefinition, DateTime, IEnumerable<TagValue>, TagValue> aggregateFunc) {
            return GetAggregatedData(tag, utcStartTime, utcEndTime, GetSampleInterval(utcStartTime, utcEndTime, pointCount), rawData, aggregateName, aggregateFunc);
        }

        #endregion

        #region [ Average ]

        /// <summary>
        /// Calculates the average value of the specified raw samples.
        /// </summary>
        /// <param name="tag">The tag definition for the data being aggregated.</param>
        /// <param name="utcSampleTime">The time stamp for the calcuated value.</param>
        /// <param name="rawValues">The values to calculate the average from.</param>
        /// <returns>
        /// The new tag value.
        /// </returns>
        /// <remarks>
        /// The qualilty used is the worst-case of all of the <paramref name="rawValues"/> used in the calculation.
        /// </remarks>
        private TagValue CalculateAverage(TagDefinition tag, DateTime utcSampleTime, IEnumerable<TagValue> rawValues) {
            var numericValue = rawValues.Average(x => x.NumericValue);
            var textValue = numericValue.ToString(); // TODO: Use "R" format (round trip) when supported in .NET Standard
            var status = rawValues.Aggregate(TagValueQuality.Good, (q, val) => val.Quality < q ? val.Quality : q); // Worst-case status

            return new TagValue(utcSampleTime, numericValue, textValue, status, tag.Units);
        }


        /// <summary>
        /// Calculates average (mean) data using the specified raw data samples.
        /// </summary>
        /// <param name="tag">The tag for the data being aggregated.</param>
        /// <param name="utcStartTime">The UTC start time for the aggregated data set.</param>
        /// <param name="utcEndTime">The UTC end time for the aggregated data set.</param>
        /// <param name="sampleInterval">The sample interval to use for the aggregation..</param>
        /// <param name="rawData">The raw samples to use in the aggregation.  Note that, for accurate calculation, <paramref name="rawData"/> should contain samples starting at <paramref name="utcStartTime"/> - <paramref name="sampleInterval"/></param>
        /// <returns>
        /// The average tag values.
        /// </returns>
        public IEnumerable<TagValue> Average(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, IEnumerable<TagValue> rawData) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (!CanAggregate(tag)) {
                return Interval(tag, utcStartTime, utcEndTime, sampleInterval, rawData);
            }

            return GetAggregatedData(tag, utcStartTime, utcEndTime, sampleInterval, rawData, DataQueryFunction.Average.Name, CalculateAverage);
        }


        /// <summary>
        /// Calculates average (mean) data using the specified raw data samples.
        /// </summary>
        /// <param name="utcStartTime">The UTC start time for the aggregated data set.</param>
        /// <param name="utcEndTime">The UTC end time for the aggregated data set.</param>
        /// <param name="pointCount">The point count to use for the aggregation..</param>
        /// <param name="rawData">The raw samples to use in the aggregation.  Note that, for accurate calculation, <paramref name="rawData"/> should contain samples starting at <paramref name="utcStartTime"/> - <paramref name="pointCount"/></param>
        /// <returns>
        /// The average tag values.
        /// </returns>
        public IEnumerable<TagValue> Average(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, int pointCount, IEnumerable<TagValue> rawData) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (!CanAggregate(tag)) {
                return Interval(tag, utcStartTime, utcEndTime, pointCount, rawData);
            }

            return GetAggregatedData(tag, utcStartTime, utcEndTime, pointCount, rawData, DataQueryFunction.Average.Name, CalculateAverage);
        }

        #endregion

        #region [ Min ]

        /// <summary>
        /// Calculates the minimum value of the specified raw samples.
        /// </summary>
        /// <param name="tag">The tag definition for the data being aggregated.</param>
        /// <param name="utcSampleTime">The time stamp for the calcuated value.</param>
        /// <param name="rawValues">The values to calculate the average from.</param>
        /// <returns>
        /// The new tag value.
        /// </returns>
        /// <remarks>
        /// The qualilty used is the worst-case of all of the <paramref name="rawValues"/> used in the calculation.
        /// </remarks>
        private TagValue CalculateMinimum(TagDefinition tag, DateTime utcSampleTime, IEnumerable<TagValue> rawValues) {
            var numericValue = rawValues.Min(x => x.NumericValue);
            var textValue = numericValue.ToString(); // TODO: Use "R" format (round trip) when supported in .NET Standard
            var status = rawValues.Aggregate(TagValueQuality.Good, (q, val) => val.Quality < q ? val.Quality : q); // Worst-case status

            return new TagValue(utcSampleTime, numericValue, textValue, status, tag.Units);
        }


        /// <summary>
        /// Calculates minimum-aggregated data using the specified raw data samples.
        /// </summary>
        /// <param name="tag">The tag definition for the data being aggregated.</param>
        /// <param name="utcStartTime">The UTC start time for the aggregated data set.</param>
        /// <param name="utcEndTime">The UTC end time for the aggregated data set.</param>
        /// <param name="sampleInterval">The sample interval to use for the aggregation.</param>
        /// <param name="rawData">The raw samples to use in the aggregation.  Note that, for accurate calculation, <paramref name="rawData"/> should contain samples starting at <paramref name="utcStartTime"/> - <paramref name="pointCount"/></param>
        /// <returns>
        /// The average tag values.
        /// </returns>
        public IEnumerable<TagValue> Minimum(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, IEnumerable<TagValue> rawData) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (!CanAggregate(tag)) {
                return Interval(tag, utcStartTime, utcEndTime, sampleInterval, rawData);
            }

            return GetAggregatedData(tag, utcStartTime, utcEndTime, sampleInterval, rawData, DataQueryFunction.Minimum.Name, CalculateMinimum);
        }


        /// <summary>
        /// Calculates minimum-aggregated data using the specified raw data samples.
        /// </summary>
        /// <param name="tag">The tag definition for the data being aggregated.</param>
        /// <param name="utcStartTime">The UTC start time for the aggregated data set.</param>
        /// <param name="utcEndTime">The UTC end time for the aggregated data set.</param>
        /// <param name="pointCount">The point count to use for the aggregation.</param>
        /// <param name="rawData">The raw samples to use in the aggregation.  Note that, for accurate calculation, <paramref name="rawData"/> should contain samples starting at <paramref name="utcStartTime"/> - <paramref name="pointCount"/></param>
        /// <returns>
        /// The average tag values.
        /// </returns>
        public IEnumerable<TagValue> Minimum(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, int pointCount, IEnumerable<TagValue> rawData) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (!CanAggregate(tag)) {
                return Interval(tag, utcStartTime, utcEndTime, pointCount, rawData);
            }

            return GetAggregatedData(tag, utcStartTime, utcEndTime, pointCount, rawData, DataQueryFunction.Minimum.Name, CalculateMinimum);
        }

        #endregion

        #region [ Max ]

        /// <summary>
        /// Calculates the maximum value of the specified raw samples.
        /// </summary>
        /// <param name="tag">The tag definition for the data being aggregated.</param>
        /// <param name="utcSampleTime">The time stamp for the calcuated value.</param>
        /// <param name="rawValues">The values to calculate the average from.</param>
        /// <returns>
        /// The new tag value.
        /// </returns>
        /// <remarks>
        /// The qualilty used is the worst-case of all of the <paramref name="rawValues"/> used in the calculation.
        /// </remarks>
        private TagValue CalculateMaximum(TagDefinition tag, DateTime utcSampleTime, IEnumerable<TagValue> rawValues) {
            var numericValue = rawValues.Max(x => x.NumericValue);
            var textValue = numericValue.ToString(); // TODO: Use "R" format (round trip) when supported in .NET Standard
            var status = rawValues.Aggregate(TagValueQuality.Good, (q, val) => val.Quality < q ? val.Quality : q); // Worst-case status

            return new TagValue(utcSampleTime, numericValue, textValue, status, tag.Units);
        }


        /// <summary>
        /// Calculates maximum-aggregated data using the specified raw data samples.
        /// </summary>
        /// <param name="tag">The tag definition for the data being aggregated.</param>
        /// <param name="utcStartTime">The UTC start time for the aggregated data set.</param>
        /// <param name="utcEndTime">The UTC end time for the aggregated data set.</param>
        /// <param name="sampleInterval">The sample interval to use for the aggregation..</param>
        /// <param name="rawData">The raw samples to use in the aggregation.  Note that, for accurate calculation, <paramref name="rawData"/> should contain samples starting at <paramref name="utcStartTime"/> - <paramref name="sampleInterval"/></param>
        /// <returns>
        /// The average tag values.
        /// </returns>
        public IEnumerable<TagValue> Maximum(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, IEnumerable<TagValue> rawData) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (!CanAggregate(tag)) {
                return Interval(tag, utcStartTime, utcEndTime, sampleInterval, rawData);
            }

            return GetAggregatedData(tag, utcStartTime, utcEndTime, sampleInterval, rawData, DataQueryFunction.Maximum.Name, CalculateMaximum);
        }


        /// <summary>
        /// Calculates maximum-aggregated data using the specified raw data samples.
        /// </summary>
        /// <param name="tag">The tag definition for the data being aggregated.</param>
        /// <param name="utcStartTime">The UTC start time for the aggregated data set.</param>
        /// <param name="utcEndTime">The UTC end time for the aggregated data set.</param>
        /// <param name="pointCount">The point count to use for the aggregation.</param>
        /// <param name="rawData">The raw samples to use in the aggregation.  Note that, for accurate calculation, <paramref name="rawData"/> should contain samples starting at <paramref name="utcStartTime"/> - <paramref name="sampleInterval"/></param>
        /// <returns>
        /// The average tag values.
        /// </returns>
        public IEnumerable<TagValue> Maximum(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, int pointCount, IEnumerable<TagValue> rawData) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (!CanAggregate(tag)) {
                return Interval(tag, utcStartTime, utcEndTime, pointCount, rawData);
            }

            return GetAggregatedData(tag, utcStartTime, utcEndTime, pointCount, rawData, DataQueryFunction.Maximum.Name, CalculateMaximum);
        }

        #endregion

        #region [ Interpolated ]

        /// <summary>
        /// Interpolates between two numeric samples.
        /// </summary>
        /// <param name="tag">The tag definition for the data being aggregated.</param>
        /// <param name="utcSampleTime">The time stamp for the interpolated sample.</param>
        /// <param name="sample0">The closest raw sample before <paramref name="utcSampleTime"/>.</param>
        /// <param name="sample1">The closest raw sample after <paramref name="utcSampleTime"/>.</param>
        /// <returns>
        /// The interpolated sample.
        /// </returns>
        private TagValue CalculateInterpolatedValue(TagDefinition tag, DateTime utcSampleTime, TagValue sample0, TagValue sample1) {
            // If either value is not numeric, we'll just return the earlier value with the requested 
            // sample time.  This is to allow "interpolation" of state values.
            if (Double.IsNaN(sample0.NumericValue) || Double.IsNaN(sample1.NumericValue) || Double.IsInfinity(sample0.NumericValue) || Double.IsInfinity(sample1.NumericValue)) {
                return new TagValue(utcSampleTime, sample0.NumericValue, sample0.TextValue, sample0.Quality, tag.Units);
            }

            var x0 = sample0.UtcSampleTime.Ticks;
            var x1 = sample1.UtcSampleTime.Ticks;

            var y0 = sample0.NumericValue;
            var y1 = sample1.NumericValue;

            var nextNumericValue = y0 + (utcSampleTime.Ticks - x0) * ((y1 - y0) / (x1 - x0));
            var nextTextValue = nextNumericValue.ToString(); // TODO: Use "R" format (round trip) when supported in .NET Standard
            var nextStatusValue = new[] { sample0, sample1 }.Aggregate(TagValueQuality.Good, (q, val) => val.Quality < q ? val.Quality : q); // Worst-case status
            return new TagValue(utcSampleTime, nextNumericValue, nextTextValue, nextStatusValue, tag.Units);
        }


        /// <summary>
        /// Creates interpolated data from the specified raw values using the provived interpolation function.
        /// </summary>
        /// <param name="tag">The tag definition for the data being aggregated.</param>
        /// <param name="utcStartTime">The start time for the interpolated data set.</param>
        /// <param name="utcEndTime">The end time for the interpolated data set.</param>
        /// <param name="sampleInterval">The sample interval to use for interpolation.</param>
        /// <param name="rawData">The raw data to use in the interpolation calculations.  You should include the raw sample before or at <paramref name="utcStartTime"/>, and the raw sample at or after <paramref name="utcEndTime"/> in this set, to ensure that samples at <paramref name="utcStartTime"/> and <paramref name="utcEndTime"/> can be calculated.</param>
        /// <param name="interpolateFunction">The interpolation function to use.</param>
        /// <returns>
        /// A set of interpolated samples.
        /// </returns>
        private IEnumerable<TagValue> GetInterpolatedData(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, IEnumerable<TagValue> rawData, Func<TagDefinition, DateTime, TagValue, TagValue, TagValue> interpolateFunction) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (utcStartTime >= utcEndTime) {
                throw new ArgumentException("Start date cannot be greater than end date.", nameof(utcStartTime));
            }
            if (sampleInterval <= TimeSpan.Zero) {
                throw new ArgumentException("Sample interval must be a positive time span.", nameof(sampleInterval));
            }
            if (rawData == null) {
                throw new ArgumentNullException(nameof(rawData));
            }

            var rawSamples = rawData.Where(x => x != null).ToArray();
            if (rawSamples.Length == 0) {
                return new TagValue[0];
            }

            if (_log?.IsEnabled(LogLevel.Debug) ?? false) {
                _log.LogDebug($"[{DataQueryFunction.Interpolated}] Performing data aggregation: Start Time = {utcStartTime:yyyy-MM-ddTHH:mm:ss.fffZ}, End Time = {utcEndTime:yyyy-MM-ddTHH:mm:ss.fffZ}, Sample Interval = {sampleInterval}, Raw Data Sample Count = {rawSamples.Length}");
            }
            CheckRawDataTimeRange(DataQueryFunction.Interpolated.Name, rawSamples, utcStartTime, utcEndTime);

            // Set the initial list capacity based on the time range and sample interval.
            var capacity = (int) ((utcEndTime - utcStartTime).TotalMilliseconds / sampleInterval.TotalMilliseconds);
            var result = capacity > 0
                ? new List<TagValue>(capacity)
                : new List<TagValue>();

            var nextSampleTime = utcStartTime;
            var firstSample = rawSamples[0];

            // If the next time stamp that we have to calculate a value for is less than the first raw 
            // sample we were given, keep incrementing the time stamp by sampleInterval until we will 
            // be calculating a time stamp that is inside the boundaries of rawSamples. 
            while (nextSampleTime < firstSample.UtcSampleTime) {
                nextSampleTime = nextSampleTime.Add(sampleInterval);
            }

            // We need to keep track of the previous raw sample at all times, so that we can interpolate 
            // values between the previous raw sample and the current one.
            TagValue previousSample = null;
            // This will hold the raw sample that occurred before previousSample.  It will be used at 
            // the end if we still need to interpolate a value at utcEndTime.
            TagValue previousPreviousSample = null;

            var sampleEnumerator = rawSamples.AsEnumerable().GetEnumerator();
            while (sampleEnumerator.MoveNext()) {
                var currentSample = sampleEnumerator.Current;

                // If we have moved past utcEndTime in our raw data set, interpolate all of our 
                // remaining values until utcEndTime and then break from the loop.
                if (currentSample.UtcSampleTime > utcEndTime) {
                    while (nextSampleTime <= utcEndTime) {
                        previousPreviousSample = previousSample;
                        previousSample = interpolateFunction(tag, nextSampleTime, previousSample, currentSample);
                        result.Add(previousSample);
                        nextSampleTime = nextSampleTime.Add(sampleInterval);
                    }
                    break;
                }

                // If the current sample time is less than the next sample time that we have to interpolate 
                // at, we'll make a note of the current raw sample and move on to the next one.
                if (currentSample.UtcSampleTime < nextSampleTime) {
                    previousPreviousSample = previousSample;
                    previousSample = currentSample;
                    continue;
                }

                // If the current sample exactly matches the next sample time we have to interpolate at, 
                // or if previousSample has not been previously set, we'll add the current raw sample 
                // to our output unmodified.  previousSample can only be null here if currentSample is 
                // the first raw sample we were given, and it also has a time stamp that is greater than 
                // the utcStartTime that was passed into the method.
                if (currentSample.UtcSampleTime == nextSampleTime || previousSample == null) {
                    previousPreviousSample = previousSample;
                    previousSample = currentSample;
                    result.Add(currentSample);
                    nextSampleTime = nextSampleTime.Add(sampleInterval);
                    continue;
                }

                // If we've moved past the sample time for our next interpolated value, calculate the 
                // interpolated value for the next required time, update the next sample time, and 
                // repeat this process until the time stamp for the next interpolated value is greater 
                // than the time stamp for currentSample.
                //
                // This allows us to handle situations where we need to produce an interpolated value 
                // at a set interval, but there is a gap in raw data that is bigger than the required 
                // interval (e.g. if we are interpolating over a 5 minute interval, but there is a gap 
                // of 30 minutes between raw samples).
                while (currentSample.UtcSampleTime >= nextSampleTime) {
                    // Calculate interpolated point.
                    previousPreviousSample = previousSample;
                    previousSample = interpolateFunction(tag, nextSampleTime, previousSample, currentSample);
                    result.Add(previousSample);

                    nextSampleTime = nextSampleTime.Add(sampleInterval);
                }
            }

            // If the last interpolated point we calculated has a time stamp earlier than the requested 
            // end time (e.g. if the end time was later than the last raw sample), or if we have not 
            // calculated any values yet, we'll calculate an additional point for the utcEndTime, 
            // based on the two most-recent raw values we processed.  
            if ((previousSample != null && previousPreviousSample != null) && (result.Count == 0 || (result.Count > 0 && result.Last().UtcSampleTime < utcEndTime))) {
                result.Add(interpolateFunction(tag, utcEndTime, previousPreviousSample, previousSample));
            }

            return result;
        }


        /// <summary>
        /// Creates interpolated data from the specified raw values using the provived interpolation function.
        /// </summary>
        /// <param name="tag">The tag definition for the data being aggregated.</param>
        /// <param name="utcStartTime">The start time for the interpolated data set.</param>
        /// <param name="utcEndTime">The end time for the interpolated data set.</param>
        /// <param name="pointCount">The point count to use for interpolation.</param>
        /// <param name="rawData">The raw data to use in the interpolation calculations.  You should include the raw sample before or at <paramref name="utcStartTime"/>, and the raw sample at or after <paramref name="utcEndTime"/> in this set, to ensure that samples at <paramref name="utcStartTime"/> and <paramref name="utcEndTime"/> can be calculated.</param>
        /// <param name="interpolateFunction">The interpolation function to use.</param>
        /// <returns>
        /// A set of interpolated samples.
        /// </returns>
        private IEnumerable<TagValue> Interpolate(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, int pointCount, IEnumerable<TagValue> rawData, Func<TagDefinition, DateTime, TagValue, TagValue, TagValue> interpolateFunction) {
            return GetInterpolatedData(tag, utcStartTime, utcEndTime, GetSampleInterval(utcStartTime, utcEndTime, pointCount), rawData, interpolateFunction);
        }


        /// <summary>
        /// Creates interpolated data from the specified raw values.
        /// </summary>
        /// <param name="tag">The tag definition for the data being aggregated.</param>
        /// <param name="utcStartTime">The start time for the interpolated data set.</param>
        /// <param name="utcEndTime">The end time for the interpolated data set.</param>
        /// <param name="sampleInterval">The sample interval to use for interpolation.</param>
        /// <param name="rawData">The raw data to use in the interpolation calculations.  You should include the raw sample before or at <paramref name="utcStartTime"/>, and the raw sample at or after <paramref name="utcEndTime"/> in this set, to ensure that samples at <paramref name="utcStartTime"/> and <paramref name="utcEndTime"/> can be calculated.</param>
        /// <returns>
        /// A set of interpolated samples.
        /// </returns>
        public IEnumerable<TagValue> Interpolate(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, IEnumerable<TagValue> rawData) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (!CanAggregate(tag)) {
                return Interval(tag, utcStartTime, utcEndTime, sampleInterval, rawData);
            }

            return GetInterpolatedData(tag, utcStartTime, utcEndTime, sampleInterval, rawData, CalculateInterpolatedValue);
        }


        /// <summary>
        /// Creates interpolated data from the specified raw values.
        /// </summary>
        /// <param name="tag">The tag definition for the data being aggregated.</param>
        /// <param name="utcStartTime">The start time for the interpolated data set.</param>
        /// <param name="utcEndTime">The end time for the interpolated data set.</param>
        /// <param name="pointCount">The point count to use for interpolation.</param>
        /// <param name="rawData">The raw data to use in the interpolation calculations.  You should include the raw sample before or at <paramref name="utcStartTime"/>, and the raw sample at or after <paramref name="utcEndTime"/> in this set, to ensure that samples at <paramref name="utcStartTime"/> and <paramref name="utcEndTime"/> can be calculated.</param>
        /// <returns>
        /// A set of interpolated samples.
        /// </returns>
        public IEnumerable<TagValue> Interpolate(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, int pointCount, IEnumerable<TagValue> rawData) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (!CanAggregate(tag)) {
                return Interval(tag, utcStartTime, utcEndTime, pointCount, rawData);
            }

            return Interpolate(tag, utcStartTime, utcEndTime, pointCount, rawData, CalculateInterpolatedValue);
        }


        /// <summary>
        /// Creates a data set with a fixed sample interval from the specified raw values, using the last 
        /// raw value before each sample time as the aggregated value.  Use this method when you need to 
        /// create "aggregated" data for tags that use discrete state values (e.g. open/closed, on/off).
        /// </summary>
        /// <param name="utcStartTime">The start time for the resulting data set.</param>
        /// <param name="utcEndTime">The end time for the resulting data set.</param>
        /// <param name="sampleInterval">The sample interval to use.</param>
        /// <param name="rawData">The raw data to use in the interpolation calculations.  You should include the raw sample before or at <paramref name="utcStartTime"/> in this set, to ensure that samples at <paramref name="utcStartTime"/> and <paramref name="utcEndTime"/> can be calculated.</param>
        /// <returns>
        /// A set of equally-spaced samples.
        /// </returns>
        public IEnumerable<TagValue> Interval(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, IEnumerable<TagValue> rawData) {
            return GetInterpolatedData(tag, utcStartTime, utcEndTime, sampleInterval, rawData, (t, sampleTime, val1, val2) => new TagValue(sampleTime, val1.NumericValue, val1.TextValue, val1.Quality, t.Units));
        }


        /// <summary>
        /// Creates a data set with a fixed sample interval from the specified raw values, using the last 
        /// raw value before each sample time as the aggregated value.  Use this method when you need to 
        /// create "aggregated" data for tags that use discrete state values (e.g. open/closed, on/off).
        /// </summary>
        /// <param name="tag">The tag definition for the data being aggregated.</param>
        /// <param name="utcStartTime">The start time for the resulting data set.</param>
        /// <param name="utcEndTime">The end time for the resulting data set.</param>
        /// <param name="pointCount">The point count to use.</param>
        /// <param name="rawData">The raw data to use in the interpolation calculations.  You should include the raw sample before or at <paramref name="utcStartTime"/> in this set, to ensure that samples at <paramref name="utcStartTime"/> and <paramref name="utcEndTime"/> can be calculated.</param>
        /// <returns>
        /// A set of equally-spaced samples.
        /// </returns>
        public IEnumerable<TagValue> Interval(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, int pointCount, IEnumerable<TagValue> rawData) {
            return Interval(tag, utcStartTime, utcEndTime, GetSampleInterval(utcStartTime, utcEndTime, pointCount), rawData);
        }

        #endregion

        #region [ Plot ]

        /// <summary>
        /// Creates a plot-friendly data set suitable for trending.
        /// </summary>
        /// <param name="tag">The tag definition for the data being aggregated.</param>
        /// <param name="utcStartTime">The UTC start time for the plot data set.</param>
        /// <param name="utcEndTime">The UTC end time for the plot data set.</param>
        /// <param name="sampleInterval">The sample interval to use when computing the plot data.</param>
        /// <param name="rawData">The raw data to use in the calculations.</param>
        /// <returns>
        /// A set of triend-friendly samples.
        /// </returns>
        /// <remarks>
        /// The plot function works by collecting raw values into buckets.  Each bucket has a start time and 
        /// end time, with the total time range being 4x <paramref name="sampleInterval"/>.  The function 
        /// iterates over <paramref name="rawData"/> and adds samples to the bucket, until it encounters a 
        /// sample that has a time stamp that is too big for the bucket.  The function then takes the earliest, 
        /// latest, minimum and maximum values in the bucket, and adds them to the result data set.
        /// 
        /// It is important then to note that <see cref="Plot"/> is not guaranteed to give evenly-spaced time 
        /// stamps in the resulting data set, but instead returns a data set that contains approximately the 
        /// same number of values that would be returned in an interpolated query using the same start, end 
        /// and sample interval parameters.
        /// </remarks>
        public IEnumerable<TagValue> Plot(TagDefinition tag, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, IEnumerable<TagValue> rawData) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (!CanAggregate(tag)) {
                return Interval(tag, utcStartTime, utcEndTime, sampleInterval, rawData);
            }
            if (utcStartTime >= utcEndTime) {
                throw new ArgumentException("Start date cannot be greater than end date.", nameof(utcStartTime));
            }
            if (sampleInterval <= TimeSpan.Zero) {
                throw new ArgumentException("Sample interval must be a positive time span.", nameof(sampleInterval));
            }
            if (rawData == null) {
                throw new ArgumentNullException(nameof(rawData));
            }

            var rawSamples = rawData.Where(x => x != null).ToArray();
            if (rawSamples.Length == 0) {
                return new TagValue[0];
            }

            if (_log?.IsEnabled(LogLevel.Debug) ?? false) {
                _log.LogDebug($"[{DataQueryFunction.Plot}] Performing data aggregation: Start Time = {utcStartTime:yyyy-MM-ddTHH:mm:ss.fffZ}, End Time = {utcEndTime:yyyy-MM-ddTHH:mm:ss.fffZ}, Sample Interval = {sampleInterval}, Raw Data Sample Count = {rawSamples.Length}");
            }
            CheckRawDataTimeRange(DataQueryFunction.Plot.Name, rawSamples, utcStartTime, utcEndTime);

            // Set the initial list capacity based on the time range and sample interval.
            var result = new List<TagValue>((int) ((utcEndTime - utcStartTime).TotalMilliseconds / sampleInterval.TotalMilliseconds));

            // We will determine the values to return for the plot request by creating aggregation buckets 
            // that cover a time range that is 4x bigger than the specified sampleInterval.  For each bucket, 
            // we will add up to 4 raw samples into the resulting data set:
            //
            // * The earliest value in the bucket.
            // * The latest value in the bucket.
            // * The maximum value in the bucket.
            // * The minimum value in the bucket.
            //
            // If a sample meets more than one of the above conditions, it will only be added to the result 
            // once.

            var sampleIntervalPerBucket = TimeSpan.FromMilliseconds(sampleInterval.TotalMilliseconds * 4);

            var bucket = new AggregationBucket() {
                UtcStart = utcStartTime,
                UtcEnd = utcStartTime.Add(sampleIntervalPerBucket)
            };

            // If the initial bucket covers a period of time that starts before the raw data set that 
            // we have been given, move the start time of the bucket forward to match the first raw 
            // sample.

            var firstSample = rawSamples[0];

            if (bucket.UtcStart < firstSample.UtcSampleTime) {
                bucket.UtcStart = firstSample.UtcSampleTime;
                // Make sure that the end time of the bucket is at least equal to the start time of the bucket.
                if (bucket.UtcEnd < bucket.UtcStart) {
                    bucket.UtcEnd = bucket.UtcStart;
                }
            }

            var sampleEnumerator = rawSamples.AsEnumerable().GetEnumerator();
            // The raw sample that was processed before the current sample.  Used when we need to interpolate 
            // a value at utcStartTime or utcEndTime.
            TagValue previousSample = null;
            // The raw sample that was processed before previousSample.  Used when we need to interpolate a 
            // value at utcEndTime.
            TagValue previousPreviousSample = null;
            // An interpolated value calculated at utcStartTime.  Included in the final result when a raw 
            // sample does not exactly fall at utcStartTime.
            TagValue interpolatedStartValue = null;


            while (sampleEnumerator.MoveNext()) {
                var currentSample = sampleEnumerator.Current;

                // If we've moved past the requested end time, break from the loop.
                if (currentSample.UtcSampleTime > utcEndTime) {
                    break;
                }

                // If the current sample lies before the bucket start time, make a note of the sample 
                // for use in interpolation calculations and move on to the next sample.  This can 
                // occur when utcStartTime is greater than the start time of the raw data set.
                if (currentSample.UtcSampleTime < bucket.UtcStart) {
                    previousPreviousSample = previousSample;
                    previousSample = currentSample;
                    continue;
                }

                // If utcStartTime lies between the previous sample and the current sample, we'll interpolate a value at utcStartTime.
                if (interpolatedStartValue == null &&
                    previousSample != null &&
                    currentSample.UtcSampleTime > utcStartTime &&
                    previousSample.UtcSampleTime < utcStartTime) {
                    interpolatedStartValue = CalculateInterpolatedValue(tag, utcStartTime, previousSample, currentSample);
                }

                previousPreviousSample = previousSample;
                previousSample = currentSample;

                // If we've moved past the end of the bucket, identify the values to use for the bucket, 
                // move to the next bucket, and repeat this process until the end time for the bucket 
                // is greater than the time stamp for currentSample.
                //
                // This allows us to handle situations where there is a gap in raw data that is bigger 
                // than our bucket size (e.g. if our bucket size is 20 minutes, but there is a gap of 
                // 30 minutes between raw samples).
                while (currentSample.UtcSampleTime > bucket.UtcEnd) {
                    if (bucket.Samples.Count > 0) {
                        if (bucket.Samples.Any(x => !Double.IsNaN(x.NumericValue))) {
                            // If any of the samples are numeric, assume that we can aggregate.
                            var significantValues = new HashSet<TagValue>();
                            significantValues.Add(bucket.Samples.First());
                            significantValues.Add(bucket.Samples.Last());
                            significantValues.Add(bucket.Samples.Aggregate((a, b) => a.NumericValue <= b.NumericValue ? a : b)); // min
                            significantValues.Add(bucket.Samples.Aggregate((a, b) => a.NumericValue >= b.NumericValue ? a : b)); // max
                            foreach (var item in significantValues.OrderBy(x => x.UtcSampleTime)) {
                                result.Add(item);
                            }
                        }
                        else {
                            // We don't have any numeric values, so we have to add each value in the bucket.
                            // TODO: should we be reducing the raw points here?
                            foreach (var item in bucket.Samples) {
                                result.Add(item);
                            }
                        }
                        bucket.Samples.Clear();
                    }

                    bucket.UtcStart = bucket.UtcEnd;
                    bucket.UtcEnd = bucket.UtcStart.Add(sampleIntervalPerBucket);
                }

                bucket.Samples.Add(currentSample);
            }

            // We've moved past utcEndTime in the raw data.  If we still have any values in the bucket, 
            // identify the significant values and add them to the result.
            if (bucket.Samples.Count > 0) {
                var significantValues = new HashSet<TagValue>();
                significantValues.Add(bucket.Samples.First());
                significantValues.Add(bucket.Samples.Last());
                significantValues.Add(bucket.Samples.Aggregate((a, b) => a.NumericValue <= b.NumericValue ? a : b)); // min
                significantValues.Add(bucket.Samples.Aggregate((a, b) => a.NumericValue >= b.NumericValue ? a : b)); // max
                foreach (var item in significantValues.OrderBy(x => x.UtcSampleTime)) {
                    result.Add(item);
                }
            }

            if (result.Count == 0) {
                // Add interpolated values at utcStartTime and utcEndTime, if possible.
                if (interpolatedStartValue != null) {
                    result.Add(interpolatedStartValue);
                    // Only attempt to add a value at utcEndTime if we also have one at utcStartTime.  Otherwise, 
                    // we will be interpolating based on two values that lie before utcStartTime.
                    if (previousSample != null && previousPreviousSample != null) {
                        result.Add(CalculateInterpolatedValue(tag, utcEndTime, previousPreviousSample, previousSample));
                    }
                }
            }
            else {
                // Add the interpolated value at utcStartTime if the first value in the result set 
                // has a time stamp greater than utcStartTime.
                if (interpolatedStartValue != null && result.First().UtcSampleTime > utcStartTime) {
                    result.Insert(0, interpolatedStartValue);
                }

                // If the last value in the result set has a time stamp less than utcEndTime, re-add 
                // the last value at utcEndTime.
                if (previousSample != null && previousPreviousSample != null && result.Last().UtcSampleTime < utcEndTime) {
                    result.Add(new TagValue(utcEndTime, previousSample.NumericValue, previousSample.TextValue, previousSample.Quality, tag.Units));
                }
            }

            return result;
        }

        #endregion

        #region [ Aggregation using Data Function Names ]

        /// <summary>
        /// Aggregates raw data.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="aggregateName"></param>
        /// <param name="utcStartTime"></param>
        /// <param name="utcEndTime"></param>
        /// <param name="sampleInterval"></param>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public IEnumerable<TagValue> Aggregate(TagDefinition tag, string aggregateName, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, IEnumerable<TagValue> rawData) {
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }
            if (!IsSupportedFunction(aggregateName)) {
                throw new ArgumentException("Unsupported function.", nameof(aggregateName));
            }

            // AVG
            if (DataQueryFunction.Average.Name.Equals(aggregateName, StringComparison.OrdinalIgnoreCase)) {
                return Average(tag, utcStartTime, utcEndTime, sampleInterval, rawData);
            }
            // INTERP
            if (DataQueryFunction.Interpolated.Name.Equals(aggregateName, StringComparison.OrdinalIgnoreCase)) {
                return Interpolate(tag, utcStartTime, utcEndTime, sampleInterval, rawData);
            }
            // MAX
            if (DataQueryFunction.Maximum.Name.Equals(aggregateName, StringComparison.OrdinalIgnoreCase)) {
                return Maximum(tag, utcStartTime, utcEndTime, sampleInterval, rawData);
            }
            // MIN
            if (DataQueryFunction.Minimum.Name.Equals(aggregateName, StringComparison.OrdinalIgnoreCase)) {
                return Minimum(tag, utcStartTime, utcEndTime, sampleInterval, rawData);
            }
            // PLOT
            if (DataQueryFunction.Plot.Name.Equals(aggregateName, StringComparison.OrdinalIgnoreCase)) {
                return Plot(tag, utcStartTime, utcEndTime, sampleInterval, rawData);
            }
            // RAW
            if (DataQueryFunction.Raw.Name.Equals(aggregateName, StringComparison.OrdinalIgnoreCase)) {
                return rawData?.Where(x => x.UtcSampleTime >= utcStartTime).Where(x => x.UtcSampleTime <= utcEndTime).ToArray();
            }

            return new TagValue[0];
        }


        /// <summary>
        /// Aggregates raw data.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="aggregateName"></param>
        /// <param name="utcStartTime"></param>
        /// <param name="utcEndTime"></param>
        /// <param name="pointCount"></param>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public IEnumerable<TagValue> Aggregate(TagDefinition tag, string aggregateName, DateTime utcStartTime, DateTime utcEndTime, int pointCount, IEnumerable<TagValue> rawData) {
            if (DataQueryFunction.Raw.Name.Equals(aggregateName, StringComparison.OrdinalIgnoreCase)) {
                return rawData?.Where(x => x.UtcSampleTime >= utcStartTime).Where(x => x.UtcSampleTime <= utcEndTime).Take(pointCount).ToArray();
            }
            return Aggregate(tag, aggregateName, utcStartTime, utcEndTime, GetSampleInterval(utcStartTime, utcEndTime, pointCount), rawData);
        }


        /// <summary>
        /// Tests if the specified function name is supported by the <see cref="AggregationUtility"/>.
        /// </summary>
        /// <param name="functionName">The function name.</param>
        /// <returns>
        /// <see langword="true"/> if the function is supported; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsSupportedFunction(string functionName) {
            return DataQueryFunction.DefaultFunctions.Any(x => x.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region [ Inner Types ]

        /// <summary>
        /// Holds samples for an aggregation bucket.
        /// </summary>
        private class AggregationBucket {

            /// <summary>
            /// Gets or sets the UTC start time for the bucket.
            /// </summary>
            public DateTime UtcStart { get; set; }

            /// <summary>
            /// Gets or sets the UTC end time for the bucket.
            /// </summary>
            public DateTime UtcEnd { get; set; }

            /// <summary>
            /// The data samples in the bucket.
            /// </summary>
            private readonly List<TagValue> _samples = new List<TagValue>();

            /// <summary>
            /// Gets the data samples for the bucket.
            /// </summary>
            public ICollection<TagValue> Samples { get { return _samples; } }


            /// <summary>
            /// Gets a string representation of the bucket.
            /// </summary>
            /// <returns>
            /// A string represntation of the bucket.
            /// </returns>
            public override string ToString() {
                return $"{{ UtcStart = {UtcStart:yyyy-MM-ddTHH:mm:ss.fffZ}, UtcEnd = {UtcEnd:yyyy-MM-ddTHH:mm:ss.fffZ}, Sample Count = {Samples.Count} }}";
            }
        }

        #endregion

    }
}
