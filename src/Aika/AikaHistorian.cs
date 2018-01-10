﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aika {
    /// <summary>
    /// Aika historian class.
    /// </summary>
    public sealed class AikaHistorian {

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The back-end historian instance.
        /// </summary>
        private readonly IHistorian _historian;

        /// <summary>
        /// Gets the back-end historian instance.
        /// </summary>
        public IHistorian Historian {
            get { return _historian; }
        }

        /// <summary>
        /// Flags if <see cref="Init(CancellationToken)"/> has been successfully called.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// Flags if <see cref="Init(CancellationToken)"/> is currently executing.
        /// </summary>
        private bool _isInitializing;

        /// <summary>
        /// Gets the UTC startup time for the <see cref="AikaHistorian"/>.
        /// </summary>
        public DateTime UtcStartupTime { get; } = DateTime.UtcNow;


        /// <summary>
        /// Creates a new <see cref="AikaHistorian"/> instance.
        /// </summary>
        /// <param name="historian">The <see cref="IHistorian"/> implementation to delegate queries to.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use.</param>
        public AikaHistorian(IHistorian historian, ILoggerFactory loggerFactory) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory?.CreateLogger<AikaHistorian>();
        }


        /// <summary>
        /// Initializes the <see cref="AikaHistorian"/>.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel initialization.</param>
        /// <returns>
        /// A task that will initialize the historian.
        /// </returns>
        public async Task Init(CancellationToken cancellationToken) {
            if (_isInitialized || _isInitializing) {
                return;
            }

            _isInitializing = true;
            try {
                await RunWithImmediateCancellation(Historian.Init(cancellationToken), cancellationToken).ConfigureAwait(false);
                _isInitialized = true;
            }
            finally {
                _isInitializing = false;
            }
        }


        /// <summary>
        /// Waits for a task to complete, but immediately cancels as soon as the specified <see cref="CancellationToken"/> fires.
        /// </summary>
        /// <typeparam name="T">The return type of the task.</typeparam>
        /// <param name="task">The task to wait on.</param>
        /// <param name="cancellationToken">The cancellation token to observe.</param>
        /// <returns>
        /// The result of the <paramref name="task"/>.
        /// </returns>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> fires before <paramref name="task"/> completes.</exception>
        private async Task<T> RunWithImmediateCancellation<T>(Task<T> task, CancellationToken cancellationToken) {
            await Task.WhenAny(task, Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            return task.Result;
        }


        /// <summary>
        /// Waits for a task to complete, but immediately cancels as soon as the specified <see cref="CancellationToken"/> fires.
        /// </summary>
        /// <param name="task">The task to wait on.</param>
        /// <param name="cancellationToken">The cancellation token to observe.</param>
        /// <returns>
        /// The result of the <paramref name="task"/>.
        /// </returns>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> fires before <paramref name="task"/> completes.</exception>
        private async Task RunWithImmediateCancellation(Task task, CancellationToken cancellationToken) {
            await Task.WhenAny(task, Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }


        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the underlying historian is not ready yet.
        /// </summary>
        private void ThrowIfNotReady() {
            if (!_isInitialized || !_historian.IsInitialized) {
                if (_isInitializing) {
                    throw new InvalidOperationException(Resources.Error_HistorianStillInitializing);
                }
                else {
                    throw new InvalidOperationException(String.Format(CultureInfo.DefaultThreadCurrentCulture, Resources.Error_HistorianNotRunning, nameof(Init)));
                }
            }
        }

        #region [ Tag Searches and Data Queries ]

        /// <summary>
        /// Gets tags that match the specified search filter.
        /// </summary>
        /// <param name="identity">The identity of the calling user.</param>
        /// <param name="filter">The tag search filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return matching tag definitions.
        /// </returns>
        /// <exception cref="InvalidOperationException">The historian has not been initialized.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="filter"/> is <see langword="null"/>.</exception>
        public async Task<IEnumerable<TagDefinition>> GetTags(ClaimsPrincipal identity, TagDefinitionFilter filter, CancellationToken cancellationToken) {
            ThrowIfNotReady();
            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }
            if (filter == null) {
                throw new ArgumentNullException(nameof(filter));
            }

            var result = await RunWithImmediateCancellation(_historian.GetTags(identity, filter, cancellationToken), cancellationToken).ConfigureAwait(false);
            return result;
        }


        /// <summary>
        /// Gets the specified tag definitions.
        /// </summary>
        /// <param name="identity">The identity of the calling user.</param>
        /// <param name="tagIdsOrNames">The tags to retrieve.</param>
        /// <param name="func">A callback function that will ensure that the calling <paramref name="identity"/> has the required permissions for the tags.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the tag definitions.
        /// </returns>
        private async Task<IEnumerable<TagDefinition>> GetTags(ClaimsPrincipal identity, IEnumerable<string> tagIdsOrNames, Func<ClaimsPrincipal, IEnumerable<string>, CancellationToken, Task<IDictionary<string, bool>>> func, CancellationToken cancellationToken) {
            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            if (tagIdsOrNames == null) {
                throw new ArgumentNullException(nameof(tagIdsOrNames));
            }

            var distinctTagNames = tagIdsOrNames.Where(x => !String.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (distinctTagNames.Length == 0) {
                throw new ArgumentException(Resources.Error_AtLeastOneTagNameRequired, nameof(tagIdsOrNames));
            }

            var authorisedTagNames = func == null 
                ? distinctTagNames 
                : (await RunWithImmediateCancellation(func(identity, distinctTagNames, cancellationToken), cancellationToken).ConfigureAwait(false)).Where(x => x.Value).Select(x => x.Key).ToArray();

            if (authorisedTagNames.Length == 0) {
                return new TagDefinition[0];
            }

            var result = await RunWithImmediateCancellation(_historian.GetTags(identity, authorisedTagNames, cancellationToken), cancellationToken).ConfigureAwait(false);
            return result;
        }


        /// <summary>
        /// Gets the specified tag definitions.
        /// </summary>
        /// <param name="identity">The identity of the calling user.</param>
        /// <param name="tagIdsOrNames">The IDs or names of the tags to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the tag definitions.
        /// </returns>
        /// <exception cref="InvalidOperationException">The historian has not been initialized.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="tagIdsOrNames"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tagIdsOrNames"/> does not contain any non-null-or-empty entries.</exception>
        public Task<IEnumerable<TagDefinition>> GetTags(ClaimsPrincipal identity, IEnumerable<string> tagIdsOrNames, CancellationToken cancellationToken) {
            ThrowIfNotReady();
            return GetTags(identity, tagIdsOrNames, null, cancellationToken);
        }


        /// <summary>
        /// Gets the aggregate functions supported natively by the underlying historian.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The supported data query aggregate functions.  All functions in <see cref="DataQueryFunction.DefaultFunctions"/> 
        /// will always be included, as <see cref="AikaHistorian"/> provides a default implementation 
        /// for these functions if they are not supported natively by the underlying <see cref="IHistorian"/>.
        /// </returns>
        private async Task<IEnumerable<DataQueryFunction>> GetAvailableNativeDataQueryFunctions(CancellationToken cancellationToken) {
            return await RunWithImmediateCancellation(_historian.GetAvailableDataQueryFunctions(cancellationToken), cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets the aggregate functions supported both natively by the underlying historian and 
        /// implicitly by the <see cref="AikaHistorian"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The supported data query aggregate functions.  All functions in <see cref="DataQueryFunction.DefaultFunctions"/> 
        /// will always be included, as <see cref="AikaHistorian"/> provides a default implementation 
        /// for these functions if they are not supported natively by the underlying <see cref="IHistorian"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">The historian has not been initialized.</exception>
        public async Task<IEnumerable<DataQueryFunction>> GetAvailableDataQueryFunctions(CancellationToken cancellationToken) {
            ThrowIfNotReady();
            var nativeFunctions = await GetAvailableNativeDataQueryFunctions(cancellationToken).ConfigureAwait(false);
            return nativeFunctions.Concat(DataQueryFunction.DefaultFunctions).Distinct().ToArray();
        }


        /// <summary>
        /// Performs a request for raw, unprocessed tag data.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The names of the tags to query.</param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="pointCount">
        ///   The maximum number of samples to retrieve.  When less than one, all samples within the 
        ///   specified time range should be retrieved.  Note that the underlying <see cref="IHistorian"/> 
        ///   may place its own limit on the maximum number of samples that will be returned.
        /// </param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that returns a map from tag name to tag values.
        /// </returns>
        /// <exception cref="InvalidOperationException">The historian has not been initialized.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="tagNames"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tagNames"/> does not contain any non-null-or-empty entries.</exception>
        /// <exception cref="ArgumentException"><paramref name="utcStartTime"/> is greater than <paramref name="utcEndTime"/>.</exception>
        public async Task<IDictionary<string, TagValueCollection>> ReadRawData(ClaimsPrincipal identity, IEnumerable<string> tagNames, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            if (tagNames == null) {
                throw new ArgumentNullException(nameof(tagNames));
            }

            if (utcStartTime > utcEndTime) {
                throw new ArgumentException(Resources.Error_StartTimeCannotBeLaterThanEndTime, nameof(utcStartTime));
            }

            var distinctTagNames = tagNames.Where(x => !String.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (distinctTagNames.Length == 0) {
                throw new ArgumentException(Resources.Error_AtLeastOneTagNameRequired, nameof(tagNames));
            }

            var authResult = await RunWithImmediateCancellation(_historian.CanReadTagData(identity, distinctTagNames, cancellationToken), cancellationToken).ConfigureAwait(false);

            var authorisedTagNames = authResult.Where(x => x.Value).Select(x => x.Key).ToArray();
            var unauthorisedTagNames = authResult.Where(x => !x.Value).Select(x => x.Key).ToArray();

            IDictionary<string, TagValueCollection> result;

            if (authorisedTagNames.Length == 0) {
                result = new Dictionary<string, TagValueCollection>();
            }
            else {
                result = await RunWithImmediateCancellation(_historian.ReadRawData(identity, authorisedTagNames, utcStartTime, utcEndTime, pointCount < 0 ? 0 : pointCount, cancellationToken), cancellationToken).ConfigureAwait(false);
            }

            foreach (var tagName in unauthorisedTagNames) {
                result[tagName] = new TagValueCollection() {
                    VisualizationHint = TagValueCollectionVisualizationHint.TrailingEdge,
                    Values = new[] {
                        TagValue.CreateUnauthorizedTagValue(utcStartTime)
                    }
                };
            }

            return result;
        }


        public async Task<IDictionary<string, TagValueCollection>> ReadPlotData(ClaimsPrincipal identity, IEnumerable<string> tagNames, DateTime utcStartTime, DateTime utcEndTime, int intervals, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            if (tagNames == null) {
                throw new ArgumentNullException(nameof(tagNames));
            }

            if (utcStartTime > utcEndTime) {
                throw new ArgumentException(Resources.Error_StartTimeCannotBeLaterThanEndTime, nameof(utcStartTime));
            }

            if (intervals < 1) {
                throw new ArgumentException(Resources.Error_PositivePointCountIsRequired, nameof(intervals));
            }

            var distinctTagNames = tagNames.Where(x => !String.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (distinctTagNames.Length == 0) {
                throw new ArgumentException(Resources.Error_AtLeastOneTagNameRequired, nameof(tagNames));
            }

            var authResult = await RunWithImmediateCancellation(_historian.CanReadTagData(identity, distinctTagNames, cancellationToken), cancellationToken).ConfigureAwait(false);

            var authorisedTagNames = authResult.Where(x => x.Value).Select(x => x.Key).ToArray();
            var unauthorisedTagNames = authResult.Where(x => !x.Value).Select(x => x.Key).ToArray();

            IDictionary<string, TagValueCollection> result;

            if (authorisedTagNames.Length == 0) {
                result = new Dictionary<string, TagValueCollection>(StringComparer.OrdinalIgnoreCase);
            }
            else {
                result = await RunWithImmediateCancellation(_historian.ReadPlotData(identity, authorisedTagNames, utcStartTime, utcEndTime, intervals, cancellationToken), cancellationToken).ConfigureAwait(false);
            }

            foreach (var tagName in unauthorisedTagNames) {
                result[tagName] = new TagValueCollection() {
                    VisualizationHint = TagValueCollectionVisualizationHint.TrailingEdge,
                    Values = new[] {
                        TagValue.CreateUnauthorizedTagValue(utcStartTime)
                    }
                };
            }

            return result;
        }


        /// <summary>
        /// Performs a historical data query.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The tags to query.  These will be separated into authorized and unauthorized sets by the method.</param>
        /// <param name="dataFunction">
        ///   The aggregate data function to use.  This must be either supported natively by the 
        ///   underlying <see cref="IHistorian"/>, or implicitly by the <see cref="AikaHistorian"/>.  
        ///   See <see cref="DataQueryFunction.DefaultFunctions"/> for implicitly supported functions.
        /// </param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="sampleInterval">
        ///   The sample interval to use for aggregation.  This is only used if <paramref name="pointCount"/> 
        ///   is <see langword="null"/>.
        /// </param>
        /// <param name="pointCount">
        ///   The sample count to use.  Note that the underlying <see cref="IHistorian"/> itself 
        ///   may place additional constraints on the maximum value of this parameter.  
        ///   Specify <see langword="null"/> to use the <paramref name="sampleInterval"/> 
        ///   instead.
        /// </param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The results of the query, indexed by tag name.
        /// </returns>
        private async Task<IDictionary<string, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<string> tagNames, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, int? pointCount, CancellationToken cancellationToken) {
            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            if (tagNames == null) {
                throw new ArgumentNullException(nameof(tagNames));
            }

            if (String.IsNullOrWhiteSpace(dataFunction)) {
                throw new ArgumentException(Resources.Error_DataFunctionNameIsRequired, nameof(dataFunction));
            }

            var distinctTagNames = tagNames.Where(x => !String.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (distinctTagNames.Length == 0) {
                throw new ArgumentException(Resources.Error_AtLeastOneTagNameRequired, nameof(tagNames));
            }

            if (utcStartTime > utcEndTime) {
                throw new ArgumentException(Resources.Error_StartTimeCannotBeLaterThanEndTime, nameof(utcStartTime));
            }

            var authResult = await RunWithImmediateCancellation(_historian.CanReadTagData(identity, distinctTagNames, cancellationToken), cancellationToken).ConfigureAwait(false);

            var authorisedTagNames = authResult.Where(x => x.Value).Select(x => x.Key).ToArray();
            var unauthorisedTagNames = authResult.Where(x => !x.Value).Select(x => x.Key).ToArray();

            IDictionary<string, TagValueCollection> result; 

            if (authorisedTagNames.Length == 0) {
                result = new Dictionary<string, TagValueCollection>(StringComparer.OrdinalIgnoreCase);
            }
            else {
                var supportedDataFunctions = await GetAvailableNativeDataQueryFunctions(cancellationToken).ConfigureAwait(false);
                var func = supportedDataFunctions?.FirstOrDefault(x => String.Equals(dataFunction, x.Name, StringComparison.OrdinalIgnoreCase));

                Task<IDictionary<string, TagValueCollection>> query;

                if (func != null) {
                    // For functons that are supported by the underlying historian, delegate the 
                    // request to the there.

                    query = pointCount.HasValue
                        ? _historian.ReadProcessedData(identity, authorisedTagNames, dataFunction, utcStartTime, utcEndTime, pointCount.Value, cancellationToken)
                        : _historian.ReadProcessedData(identity, authorisedTagNames, dataFunction, utcStartTime, utcEndTime, sampleInterval, cancellationToken);
                }
                else if (AggregationUtility.IsSupportedFunction(dataFunction)) {
                    // For functions that are not supported by the underlying historian but are 
                    // supported by AggregationUtility, we'll request raw data and then perform 
                    // the aggregation here.

                    // The aggregation helper requires the definitions of the tags that we are 
                    // processing.
                    var tags = await GetTags(identity, authorisedTagNames, cancellationToken).ConfigureAwait(false);

                    // We need to subtract one interval from the query start time for the raw data.  
                    // This is so that we can calculate an aggregated value at the utcStartTime that 
                    // the caller specified.

                    if (pointCount.HasValue) {
                        sampleInterval = TimeSpan.FromMilliseconds((utcEndTime - utcStartTime).TotalMilliseconds / pointCount.Value);
                    }
                    var queryStartTime = utcStartTime.Subtract(sampleInterval);

                    query = _historian.ReadRawData(identity, authorisedTagNames, queryStartTime, utcEndTime, 0, cancellationToken).ContinueWith(t => {
                        var aggregator = new AggregationUtility(_loggerFactory);
                        // We'll hint that INTERP data can be interpolated on a chart, but 
                        // other functions should use trailing-edge transitions.
                        var visualizationHint = DataQueryFunction.Interpolated.Equals(dataFunction, StringComparison.OrdinalIgnoreCase)
                            ? TagValueCollectionVisualizationHint.Interpolated
                            : TagValueCollectionVisualizationHint.TrailingEdge;

                        var aggregatedData = t.Result
                                              .ToDictionary(x => x.Key, 
                                                            x => new TagValueCollection() {
                                                                Values = aggregator.Aggregate(tags.First(tag => tag.Name.Equals(x.Key, StringComparison.OrdinalIgnoreCase)), dataFunction, utcStartTime, utcEndTime, sampleInterval, x.Value.Values),
                                                                VisualizationHint = visualizationHint
                                                            });
                        return (IDictionary<string, TagValueCollection>) aggregatedData;
                    }, cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Current);
                }
                else {
                    // The underlying historian does not support the function, and neither do we.
                    var vals = authorisedTagNames.ToDictionary(x => x,
                                                               x => new TagValueCollection() {
                                                                   VisualizationHint = TagValueCollectionVisualizationHint.TrailingEdge,
                                                                   Values = new[] {
                                                                       new TagValue(utcStartTime, Double.NaN, Resources.Error_UnsupportedDataFunction, TagValueQuality.Bad, null)
                                                                   }
                                                               });
                    query = Task.FromResult<IDictionary<string, TagValueCollection>>(vals);
                }

                result = await RunWithImmediateCancellation(query, cancellationToken);
            }

            foreach (var tagName in unauthorisedTagNames) {
                result[tagName] = new TagValueCollection() {
                    VisualizationHint = TagValueCollectionVisualizationHint.TrailingEdge,
                    Values = new[] {
                        TagValue.CreateUnauthorizedTagValue(utcStartTime)
                    }
                };
            }

            return result;
        }


        /// <summary>
        /// Performs an aggregated data query.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The tag names to query.</param>
        /// <param name="dataFunction">
        ///   The aggregate data function to use.  Call <see cref="GetAvailableDataQueryFunctions(CancellationToken)"/> 
        ///   to get the available function names.
        /// </param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="sampleInterval">The sample interval for the query.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return a map from tag name to tag values.
        /// </returns>
        /// <exception cref="InvalidOperationException">The historian has not been initialized.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="tagNames"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tagNames"/> does not contain any non-null-or-empty entries.</exception>
        /// <exception cref="ArgumentException"><paramref name="dataFunction"/> is <see langword="null"/> or white space.</exception>
        /// <exception cref="ArgumentException"><paramref name="dataFunction"/> is not supported.</exception>
        /// <exception cref="ArgumentException"><paramref name="utcStartTime"/> is greater than <paramref name="utcEndTime"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="sampleInterval"/> is less than or equal to <see cref="TimeSpan.Zero"/>.</exception>
        public Task<IDictionary<string, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<string> tagNames, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, CancellationToken cancellationToken) {
            ThrowIfNotReady();
            if (sampleInterval <= TimeSpan.Zero) {
                throw new ArgumentException(Resources.Error_SampleIntervalMustBeGreaterThanZero, nameof(sampleInterval));
            }
            return ReadProcessedData(identity, tagNames, dataFunction, utcStartTime, utcEndTime, sampleInterval, null, cancellationToken);
        }


        /// <summary>
        /// Performs an aggregated data query.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The tag names to query.</param>
        /// <param name="dataFunction">
        ///   The aggregate data function to use.  Call <see cref="GetAvailableDataQueryFunctions(CancellationToken)"/> 
        ///   to get the available function names.
        /// </param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="pointCount">
        ///   The sample count to use.  Note that the underlying <see cref="IHistorian"/> 
        ///   may place its own limit on the maximum number of samples that will be returned.
        /// </param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return a map from tag name to tag values.
        /// </returns>
        /// <exception cref="InvalidOperationException">The historian has not been initialized.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="tagNames"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tagNames"/> does not contain any non-null-or-empty entries.</exception>
        /// <exception cref="ArgumentException"><paramref name="dataFunction"/> is <see langword="null"/> or white space.</exception>
        /// <exception cref="ArgumentException"><paramref name="dataFunction"/> is not supported.</exception>
        /// <exception cref="ArgumentException"><paramref name="utcStartTime"/> is greater than <paramref name="utcEndTime"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="pointCount"/> is less than one.</exception>
        public Task<IDictionary<string, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<string> tagNames, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken) {
            ThrowIfNotReady();
            if (pointCount < 1) {
                throw new ArgumentException(Resources.Error_PositivePointCountIsRequired, nameof(pointCount));
            }
            return ReadProcessedData(identity, tagNames, dataFunction, utcStartTime, utcEndTime, TimeSpan.Zero, pointCount, cancellationToken);
        }


        /// <summary>
        /// Performs a snapshot data query.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The tag names to query.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return a map from tag name to tag values.
        /// </returns>
        /// <exception cref="InvalidOperationException">The historian has not been initialized.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="tagNames"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tagNames"/> does not contain any non-null-or-empty entries.</exception>
        public async Task<IDictionary<string, TagValue>> ReadSnapshotData(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            if (tagNames == null) {
                throw new ArgumentNullException(nameof(tagNames));
            }

            var distinctTagNames = tagNames.Where(x => !String.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (distinctTagNames.Length == 0) {
                throw new ArgumentException(Resources.Error_AtLeastOneTagNameRequired, nameof(tagNames));
            }

            var authResult = await RunWithImmediateCancellation(_historian.CanReadTagData(identity, distinctTagNames, cancellationToken), cancellationToken).ConfigureAwait(false);

            var authorisedTagNames = authResult.Where(x => x.Value).Select(x => x.Key).ToArray();
            var unauthorisedTagNames = authResult.Where(x => !x.Value).Select(x => x.Key).ToArray();

            IDictionary<string, TagValue> result;

            if (authorisedTagNames.Length == 0) {
                result = new Dictionary<string, TagValue>();
            }
            else {
                var query = _historian.ReadSnapshotData(identity, authorisedTagNames, cancellationToken);
                result = await RunWithImmediateCancellation(query, cancellationToken);
            }

            foreach (var tagName in unauthorisedTagNames) {
                result[tagName] = TagValue.CreateUnauthorizedTagValue(DateTime.MinValue);
            }

            return result;
        }


        /// <summary>
        /// Creates a new snapshot subscription.
        /// </summary>
        /// <param name="identity">The identity that the subscription will be associated with.</param>
        /// <returns>
        /// A new <see cref="SnapshotSubscription"/> object.  It is important that this subscription 
        /// object is not shared between multiple identities, as all subscribers to the 
        /// <see cref="SnapshotSubscription.ValuesReceived"/> event will receive snapshot value 
        /// changes as they occur.
        /// </returns>
        /// <exception cref="InvalidOperationException">The historian has not been initialized.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        public SnapshotSubscription CreateSnapshotSubscription(ClaimsPrincipal identity) {
            ThrowIfNotReady();

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            return new SnapshotSubscription(this);
        }

        #endregion

        #region [ Tag Data Writes ]

        /// <summary>
        /// For a set of tag names, gets the tag definition objects that the caller is authorised to write to.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The tag names to test.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the tag definitions that the caller is allowed to write to.
        /// </returns>
        private Task<IEnumerable<TagDefinition>> GetWriteableTags(ClaimsPrincipal identity, IEnumerable<string> tagNames, CancellationToken cancellationToken) {
            var task = GetTags(identity,
                               tagNames,
                               (id, n, ct) => _historian.CanWriteTagData(id, n, ct),
                               cancellationToken);

            return RunWithImmediateCancellation(task, cancellationToken);
        }


        /// <summary>
        /// Inserts tag data into the historian archive.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="values">The values to write, indexed by tag name.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The results of the insert, indexed by tag name.
        /// </returns>
        /// <exception cref="InvalidOperationException">The historian has not been initialized.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="values"/> does not contain any entries.</exception>
        /// <exception cref="SecurityException">The calling <paramref name="identity"/> is not authorized to write tag data.</exception>
        public async Task<IDictionary<string, WriteTagValuesResult>> InsertTagArchiveData(ClaimsPrincipal identity, IDictionary<string, IEnumerable<TagValue>> values, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }

            if (values.Count == 0) {
                throw new ArgumentException(Resources.Error_AtLeastOneTagNameRequired, nameof(values));
            }

            var result = new ConcurrentDictionary<string, WriteTagValuesResult>(StringComparer.OrdinalIgnoreCase);

            // Get definitions of the tags to write data to, where the caller has permission to write 
            // to the tag and the values dictionary contains at least one value for the tag.
            var tagNames = values.Where(x => x.Value != null && x.Value.Any(v => v != null)).Select(x => x.Key);

            var tags = await GetWriteableTags(identity, tagNames, cancellationToken).ConfigureAwait(false);
            var unauthorizedTags = tagNames.Except(tags.Select(x => x.Name)).ToArray();

            foreach (var item in unauthorizedTags) {
                result[item] = WriteTagValuesResult.CreateUnauthorizedResult();
            }

            if (!tags.Any()) {
                return result;
            }

            var tasks = new List<Task>(values.Count);

            foreach (var item in values) {
                var tag = tags.FirstOrDefault(x => String.Equals(x.Name, item.Key, StringComparison.OrdinalIgnoreCase));
                if (tag == null) {
                    continue;
                }

                var valuesToInsert = item.Value.Where(x => x != null).OrderBy(x => x.UtcSampleTime).ToArray();
                if (valuesToInsert.Length == 0) {
                    continue;
                }

                tasks.Add(Task.Run(async () => {
                    try {
                        result[tag.Name] = await tag.InsertArchiveValues(identity, valuesToInsert, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception e) {
                        result[tag.Name] = new WriteTagValuesResult(false, 0, null, null, new[] { e.Message });
                    }
                }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return result;
        }


        /// <summary>
        /// Writes snapshot data to the Aika historian i.e. data that represents changes in the 
        /// instantaneous value of instruments, or a process.  Data passed via this method will
        /// be filtered according to the exception and compression settings for the destination
        /// tags to determine if it should be archived or discarded.  The snapshot value of the 
        /// tags will always be updated, as long as the <paramref name="values"/> specified are 
        /// newer than the current snapshot values of the destination tags.
        /// </summary>
        /// <param name="identity">The identity of the calling user.</param>
        /// <param name="values">The values to write, indexed by tag name.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The results of the write, indexed by tag name.
        /// </returns>
        /// <exception cref="InvalidOperationException">The historian has not been initialized.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="values"/> does not contain any entries.</exception>
        /// <exception cref="SecurityException">The calling <paramref name="identity"/> is not authorized to write tag data.</exception>
        public async Task<IDictionary<string, WriteTagValuesResult>> WriteTagData(ClaimsPrincipal identity, IDictionary<string, IEnumerable<TagValue>> values, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }

            if (values.Count == 0) {
                throw new ArgumentException(Resources.Error_AtLeastOneTagNameRequired, nameof(values));
            }

            var result = new ConcurrentDictionary<string, WriteTagValuesResult>(StringComparer.OrdinalIgnoreCase);

            // Get definitions of the tags to write data to, where the caller has permission to write 
            // to the tag and the values dictionary contains at least one value for the tag.
            var tagNames = values.Where(x => x.Value != null && x.Value.Any(v => v != null)).Select(x => x.Key);

            var tags = await GetWriteableTags(identity, tagNames, cancellationToken).ConfigureAwait(false);
            var unauthorizedTags = tagNames.Except(tags.Select(x => x.Name)).ToArray();

            foreach (var item in unauthorizedTags) {
                result[item] = WriteTagValuesResult.CreateUnauthorizedResult();
            }

            if (!tags.Any()) {
                return result;
            }

            var valuesToForward = new Dictionary<string, IEnumerable<TagValue>>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in values) {
                var tag = tags.FirstOrDefault(x => String.Equals(x.Name, item.Key, StringComparison.OrdinalIgnoreCase));
                if (tag == null) {
                    continue;
                }

                result[tag.Name] = await tag.WriteSnapshotValues(identity, item.Value, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }

        #endregion

        #region [ Tag Management ]

        /// <summary>
        /// Gets the number of tags in the historian.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the number of tags in the historian.  Note that the underlying 
        /// <see cref="IHistorian"/> may choose to return <see langword="null"/> instead of providing 
        /// a tag count.
        /// </returns>
        /// <exception cref="InvalidOperationException">The historian has not been initialized.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        public async Task<int?> GetTagCount(ClaimsPrincipal identity, CancellationToken cancellationToken) {
            ThrowIfNotReady();
            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }
            return await _historian.GetTagCount(identity, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Validates the specified exception filter settings.
        /// </summary>
        /// <param name="dataType">The data type of the tag that the settings are for.</param>
        /// <param name="settings">The settings to validate.</param>
        /// <returns>
        /// A validated <see cref="TagValueFilterSettingsUpdate"/> instance.
        /// </returns>
        private TagValueFilterSettingsUpdate ValidateExceptionFilterSettings(TagDataType dataType, TagValueFilterSettingsUpdate settings) {
            return new TagValueFilterSettingsUpdate() {
                IsEnabled = settings.IsEnabled,
                LimitType = dataType == TagDataType.State || dataType == TagDataType.Text
                    ? TagValueFilterDeviationType.Absolute 
                    : settings.LimitType ?? TagValueFilterDeviationType.Absolute,
                Limit = dataType == TagDataType.State
                        ? 0.5
                        : settings.Limit ?? 0,
                WindowSize = settings.WindowSize ?? TagValueFilterSettings.DefaultWindowSize
            };
        }


        /// <summary>
        /// Validates the specified compression filter settings.
        /// </summary>
        /// <param name="dataType">The data type of the tag that the settings are for.</param>
        /// <param name="settings">The settings to validate.</param>
        /// <returns>
        /// A validated <see cref="TagValueFilterSettingsUpdate"/> instance.
        /// </returns>
        private TagValueFilterSettingsUpdate ValidateCompressionFilterSettings(TagDataType dataType, TagValueFilterSettingsUpdate settings) {
            // Disable compression for state and text tags.
            if (dataType == TagDataType.State || dataType == TagDataType.Text) {
                return new TagValueFilterSettingsUpdate() {
                    IsEnabled = false
                };
            }

            return new TagValueFilterSettingsUpdate() {
                IsEnabled = settings.IsEnabled,
                LimitType = settings.LimitType ?? TagValueFilterDeviationType.Absolute,
                Limit = settings.Limit ?? 0,
                WindowSize = settings.WindowSize ?? TagValueFilterSettings.DefaultWindowSize
            };
        }


        /// <summary>
        /// Gets default exception filter settings for the specified tag data type.
        /// </summary>
        /// <param name="dataType">The tag data type.</param>
        /// <returns>
        /// The default <see cref="TagValueFilterSettingsUpdate"/> for the data type.
        /// </returns>
        private TagValueFilterSettingsUpdate GetDefaultExceptionFilterSettings(TagDataType dataType) {
            return new TagValueFilterSettingsUpdate() {
                IsEnabled = true,
                LimitType = TagValueFilterDeviationType.Absolute,
                Limit = 0,
                WindowSize = TagValueFilterSettings.DefaultWindowSize
            };
        }


        /// <summary>
        /// Gets default compression filter settings for the specified tag data type.
        /// </summary>
        /// <param name="dataType">The tag data type.</param>
        /// <returns>
        /// The default <see cref="TagValueFilterSettingsUpdate"/> for the data type.
        /// </returns>
        private TagValueFilterSettingsUpdate GetDefaultCompressionFilterSettings(TagDataType dataType) {
            // Disable compression for state and text tags.
            if (dataType == TagDataType.State || dataType == TagDataType.Text) {
                return new TagValueFilterSettingsUpdate() {
                    IsEnabled = false
                };
            }

            return new TagValueFilterSettingsUpdate() {
                IsEnabled = true,
                LimitType = TagValueFilterDeviationType.Absolute,
                Limit = 0,
                WindowSize = TagValueFilterSettings.DefaultWindowSize
            };
        }


        /// <summary>
        /// Creates a new tag definition.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="settings">The tag settings.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the new tag definition.
        /// </returns>
        /// <exception cref="InvalidOperationException">The historian has not been initialized.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
        public async Task<TagDefinition> CreateTag(ClaimsPrincipal identity, TagSettings settings, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            if (settings == null) {
                throw new ArgumentNullException(nameof(settings));
            }

            settings = new TagSettings(settings);

            if (settings.ExceptionFilterSettings != null) {
                settings.ExceptionFilterSettings = ValidateExceptionFilterSettings(settings.DataType, settings.ExceptionFilterSettings);
            }
            else {
                settings.ExceptionFilterSettings = GetDefaultExceptionFilterSettings(settings.DataType);
            }

            if (settings.CompressionFilterSettings != null) {
                settings.CompressionFilterSettings = ValidateCompressionFilterSettings(settings.DataType, settings.CompressionFilterSettings);
            }
            else {
                settings.CompressionFilterSettings = GetDefaultCompressionFilterSettings(settings.DataType);
            }

            return await _historian.CreateTag(identity, settings, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Updates an existing tag definition.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagId">The ID of the tag to update.</param>
        /// <param name="settings">The updated tag settings.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the updated tag definition.
        /// </returns>
        /// <exception cref="InvalidOperationException">The historian has not been initialized.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tagId"/> is <see langword="null"/> or white space.</exception>
        public async Task<TagDefinition> UpdateTag(ClaimsPrincipal identity, string tagId, TagSettings settings, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            if (String.IsNullOrWhiteSpace(tagId)) {
                throw new ArgumentException(Resources.Error_TagIdIsRequired, nameof(tagId));
            }

            if (settings == null) {
                throw new ArgumentNullException(nameof(settings));
            }

            if (settings.ExceptionFilterSettings != null) {
                settings.ExceptionFilterSettings = ValidateExceptionFilterSettings(settings.DataType, settings.ExceptionFilterSettings);
            }

            if (settings.CompressionFilterSettings != null) {
                settings.CompressionFilterSettings = ValidateCompressionFilterSettings(settings.DataType, settings.CompressionFilterSettings);
            }

            return await _historian.UpdateTag(identity, tagId, settings, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Deletes a tag definition.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagIdOrName">The ID or name of the tag to delete.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that returns a flag indicating if the delete was successful.
        /// </returns>
        /// <exception cref="InvalidOperationException">The historian has not been initialized.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tagIdOrName"/> is <see langword="null"/> or white space.</exception>
        public async Task<bool> DeleteTag(ClaimsPrincipal identity, string tagIdOrName, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            if (String.IsNullOrWhiteSpace(tagIdOrName)) {
                throw new ArgumentException(Resources.Error_TagIdOrNameIsRequired, nameof(tagIdOrName));
            }

            return await _historian.DeleteTag(identity, tagIdOrName, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets the state sets that match the specified filter.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="filter">The search filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A collection of matching tag state sets.
        /// </returns>
        /// <exception cref="InvalidOperationException">The historian has not been initialized.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="filter"/> is <see langword="null"/>.</exception>
        public async Task<IEnumerable<StateSet>> GetStateSets(ClaimsPrincipal identity, StateSetFilter filter, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            if (filter == null) {
                throw new ArgumentNullException(nameof(filter));
            }

            return await _historian.GetStateSets(identity, filter, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets the tag state set with the specified name.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="name">The name of the state set.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the matching state set.
        /// </returns>
        /// <exception cref="InvalidOperationException">The historian has not been initialized.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or white space.</exception>
        public async Task<StateSet> GetStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }
            if (String.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException(Resources.Error_StateSetNameIsRequired, nameof(name));
            }

            return await _historian.GetStateSet(identity, name, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Creates a tag state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="settings">The state set settings.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the new state set.
        /// </returns>
        /// <exception cref="InvalidOperationException">The historian has not been initialized.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The <see cref="StateSetSettings.Name"/> in <paramref name="settings"/> is <see langword="null"/> or white space.</exception>
        /// <exception cref="ArgumentException">A state set with the same name already exists.</exception>
        /// <exception cref="ArgumentException">The <see cref="StateSetSettings.States"/> property in <paramref name="settings"/> is <see langword="null"/> or empty.</exception>
        public async Task<StateSet> CreateStateSet(ClaimsPrincipal identity, StateSetSettings settings, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            if (String.IsNullOrWhiteSpace(settings?.Name)) {
                throw new ArgumentException(Resources.Error_StateSetNameIsRequired, nameof(settings));
            }

            var nonNullStates = settings?.States?.Where(x => x != null).ToArray() ?? new StateSetItem[0];

            if (nonNullStates.Length == 0) {
                throw new ArgumentException(Resources.Error_AtLeastOneStateIsRequired, nameof(settings.States));
            }

            var existing = await _historian.GetStateSet(identity, settings.Name, cancellationToken).ConfigureAwait(false);
            if (existing != null) {
                throw new ArgumentException(Resources.Error_StateSetAlreadyExists, nameof(settings));
            }

            return await _historian.CreateStateSet(identity, new StateSetSettings() { Name = settings.Name, Description = settings.Description, States = nonNullStates }, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Updates a tag state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="name">The name of the state set.</param>
        /// <param name="settings">The state set settings.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the updated state set.
        /// </returns>
        /// <exception cref="InvalidOperationException">The historian has not been initialized.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or white space.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> not a known state set.</exception>
        /// <exception cref="ArgumentException">The <see cref="StateSetSettings.States"/> property in <paramref name="settings"/> is <see langword="null"/> or empty.</exception>
        public async Task<StateSet> UpdateStateSet(ClaimsPrincipal identity, string name, StateSetSettings settings, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            if (String.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException(Resources.Error_StateSetNameIsRequired, nameof(name));
            }

            var nonNullStates = settings?.States?.Where(x => x != null).ToArray() ?? new StateSetItem[0];

            if (nonNullStates.Length == 0) {
                throw new ArgumentException(Resources.Error_AtLeastOneStateIsRequired, nameof(settings));
            }

            var existing = await _historian.GetStateSet(identity, name, cancellationToken).ConfigureAwait(false);
            if (existing == null) {
                throw new ArgumentException(Resources.Error_StateSetDoesNotExist, nameof(name));
            }

            return await _historian.UpdateStateSet(identity, name, new StateSetSettings() { Description = settings.Description, States = nonNullStates }, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Deletes a tag state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="name">The name of the state set.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that returns a flag indicating if the state set was deleted.
        /// </returns>
        /// <exception cref="InvalidOperationException">The historian has not been initialized.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or white space.</exception>
        public async Task<bool> DeleteStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            if (String.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException(Resources.Error_StateSetNameIsRequired, nameof(name));
            }

            return await _historian.DeleteStateSet(identity, name, cancellationToken).ConfigureAwait(false);
        }

        #endregion

    }
}
