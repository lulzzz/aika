using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                    throw new InvalidOperationException("The historian is still initializing.");
                }
                else {
                    throw new InvalidOperationException($"The historian has not been initialized.  Call the {nameof(Init)} method to initialize.");
                }
            }
        }

        #region [ Tag Searches and Data Queries ]

        /// <summary>
        /// Performs a tag search.
        /// </summary>
        /// <param name="identity">The identity of the calling user.</param>
        /// <param name="filter">The tag search filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return matching tag definitions.
        /// </returns>
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
        /// <param name="tagNames">The tags to retrieve.</param>
        /// <param name="func">A callback function that will ensure that the calling <paramref name="identity"/> has the required permissions for the tags.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the tag definitions.
        /// </returns>
        private async Task<IEnumerable<TagDefinition>> GetTags(ClaimsPrincipal identity, IEnumerable<string> tagNames, Func<ClaimsPrincipal, IEnumerable<string>, CancellationToken, Task<IDictionary<string, bool>>> func, CancellationToken cancellationToken) {
            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            if (tagNames == null) {
                throw new ArgumentNullException(nameof(tagNames));
            }

            var distinctTagNames = tagNames.Where(x => !String.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (distinctTagNames.Length == 0) {
                throw new ArgumentException("You must specify at least one non-empty tag name.", nameof(tagNames));
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
        public async Task<IEnumerable<DataQueryFunction>> GetAvailableDataQueryFunctions(CancellationToken cancellationToken) {
            ThrowIfNotReady();
            var nativeFunctions = await GetAvailableNativeDataQueryFunctions(cancellationToken).ConfigureAwait(false);
            return nativeFunctions.Concat(DataQueryFunction.DefaultFunctions).Distinct().ToArray();
        }


        /// <summary>
        /// Performs a request for raw, unprocessed tag data.
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="tagNames"></param>
        /// <param name="utcStartTime"></param>
        /// <param name="utcEndTime"></param>
        /// <param name="pointCount"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IDictionary<string, TagValueCollection>> ReadRawData(ClaimsPrincipal identity, IEnumerable<string> tagNames, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            if (tagNames == null) {
                throw new ArgumentNullException(nameof(tagNames));
            }

            var distinctTagNames = tagNames.Where(x => !String.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (distinctTagNames.Length == 0) {
                throw new ArgumentException("You must specify at least one non-empty tag name.", nameof(tagNames));
            }

            var authResult = await RunWithImmediateCancellation(_historian.CanReadTagData(identity, distinctTagNames, cancellationToken), cancellationToken).ConfigureAwait(false);

            var authorisedTagNames = authResult.Where(x => x.Value).Select(x => x.Key).ToArray();
            var unauthorisedTagNames = authResult.Where(x => !x.Value).Select(x => x.Key).ToArray();

            IDictionary<string, TagValueCollection> result;

            if (authorisedTagNames.Length == 0) {
                result = new Dictionary<string, TagValueCollection>();
            }
            else {
                result = await RunWithImmediateCancellation(_historian.ReadRawData(identity, authorisedTagNames, utcStartTime, utcEndTime, pointCount, cancellationToken), cancellationToken).ConfigureAwait(false);
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

            var distinctTagNames = tagNames.Where(x => !String.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (distinctTagNames.Length == 0) {
                throw new ArgumentException("You must specify at least one non-empty tag name.", nameof(tagNames));
            }

            var authResult = await RunWithImmediateCancellation(_historian.CanReadTagData(identity, distinctTagNames, cancellationToken), cancellationToken).ConfigureAwait(false);

            var authorisedTagNames = authResult.Where(x => x.Value).Select(x => x.Key).ToArray();
            var unauthorisedTagNames = authResult.Where(x => !x.Value).Select(x => x.Key).ToArray();

            IDictionary<string, TagValueCollection> result; 

            if (authorisedTagNames.Length == 0) {
                result = new Dictionary<string, TagValueCollection>();
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

                    // We need to subtract one interval from the quesry start time for the raw data.  
                    // This is so that we can calculate an aggregated value at the utcStartTime that 
                    // the caller specified.

                    if (pointCount.HasValue) {
                        sampleInterval = TimeSpan.FromMilliseconds((utcEndTime - utcStartTime).TotalMilliseconds / pointCount.Value);
                    }
                    var queryStartTime = utcStartTime.Subtract(sampleInterval);

                    query = _historian.ReadRawData(identity, authorisedTagNames, queryStartTime, utcEndTime, 0, cancellationToken).ContinueWith(t => {
                        var aggregator = new AggregationUtility(_loggerFactory);
                        // We'll hint that PLOT and INTERP data can be interpolated on a chart, but 
                        // other functions should use trailing-edge transitions.
                        var visualizationHint = DataQueryFunction.Interpolated.Equals(dataFunction, StringComparison.OrdinalIgnoreCase) || DataQueryFunction.Plot.Equals(dataFunction, StringComparison.OrdinalIgnoreCase)
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
                                                                       new TagValue(utcStartTime, Double.NaN, "Unsupported data function", TagValueQuality.Bad, null)
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
        ///   to get the available functions.
        /// </param>
        /// <param name="utcStartTime"></param>
        /// <param name="utcEndTime"></param>
        /// <param name="sampleInterval"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<IDictionary<string, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<string> tagNames, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, CancellationToken cancellationToken) {
            ThrowIfNotReady();
            return ReadProcessedData(identity, tagNames, dataFunction, utcStartTime, utcEndTime, sampleInterval, null, cancellationToken);
        }


        public Task<IDictionary<string, TagValueCollection>> ReadProcessedData(ClaimsPrincipal identity, IEnumerable<string> tagNames, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken) {
            ThrowIfNotReady();
            return ReadProcessedData(identity, tagNames, dataFunction, utcStartTime, utcEndTime, TimeSpan.Zero, pointCount, cancellationToken);
        }


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
                throw new ArgumentException("You must specify at least one non-empty tag name.", nameof(tagNames));
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
        public SnapshotSubscription CreateSnapshotSubscription(ClaimsPrincipal identity) {
            ThrowIfNotReady();

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            return new SnapshotSubscription(this);
        }

        #endregion

        #region [ Tag Data Writes ]

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
        /// <param name="values">The values to write.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The results of the insert, indexed by tag name.
        /// </returns>
        /// <exception cref="SecurityException">The calling <paramref name="identity"/> is not authorized to write tag data.</exception>
        public async Task<IDictionary<string, WriteTagValuesResult>> InsertTagData(ClaimsPrincipal identity, IDictionary<string, IEnumerable<TagValue>> values, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }

            if (values.Count == 0) {
                throw new ArgumentException("You must specify at least one tag to write data to.", nameof(values));
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
                        result[tag.Name] = await tag.InsertArchiveValues(valuesToInsert, cancellationToken).ConfigureAwait(false);
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
        /// <param name="values">The values to write.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The results of the write, indexed by tag name.
        /// </returns>
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
                throw new ArgumentException("You must specify at least one tag to write data to.", nameof(values));
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

                result[tag.Name] = tag.WriteSnapshotValues(item.Value);
            }

            return result;
        }

        #endregion

        #region [ Tag Management ]

        public async Task<int?> GetTagCount(ClaimsPrincipal identity, CancellationToken cancellationToken) {
            ThrowIfNotReady();
            return await _historian.GetTagCount(identity, cancellationToken).ConfigureAwait(false);
        }


        private TagValueFilterSettingsUpdate GetFilterSettings(TagDataType dataType, TagValueFilterSettingsUpdate settings) {
            return new TagValueFilterSettingsUpdate() {
                IsEnabled = settings.IsEnabled,
                LimitType = settings.LimitType ?? TagValueFilterDeviationType.Absolute,
                Limit = dataType == TagDataType.State
                        ? 1
                        : settings.Limit ?? 0,
                WindowSize = settings.WindowSize ?? TagValueFilterSettings.DefaultWindowSize
            };
        }


        private TagValueFilterSettingsUpdate GetDefaultFilterSettings(TagDataType dataType) {
            return new TagValueFilterSettingsUpdate() {
                IsEnabled = true,
                LimitType = TagValueFilterDeviationType.Absolute,
                Limit = dataType == TagDataType.State
                        ? 1
                        : 0,
                WindowSize = TagValueFilterSettings.DefaultWindowSize
            };
        }


        public async Task<TagDefinition> CreateTag(ClaimsPrincipal identity, TagSettings tag, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

            tag = new TagSettings(tag);

            if (tag.ExceptionFilterSettings != null) {
                tag.ExceptionFilterSettings = GetFilterSettings(tag.DataType, tag.ExceptionFilterSettings);
            }
            else {
                tag.ExceptionFilterSettings = GetDefaultFilterSettings(tag.DataType);
            }

            if (tag.CompressionFilterSettings != null) {
                tag.CompressionFilterSettings = GetFilterSettings(tag.DataType, tag.CompressionFilterSettings);
            }
            else {
                tag.CompressionFilterSettings = GetDefaultFilterSettings(tag.DataType);
            }

            return await _historian.CreateTag(identity, tag, cancellationToken).ConfigureAwait(false);
        }


        public async Task<TagDefinition> UpdateTag(ClaimsPrincipal identity, string tagId, TagSettings tag, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (String.IsNullOrWhiteSpace(tagId)) {
                throw new ArgumentException("You must specify a tag ID.", nameof(tagId));
            }

            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

            if (tag.ExceptionFilterSettings != null) {
                tag.ExceptionFilterSettings = GetFilterSettings(tag.DataType, tag.ExceptionFilterSettings);
            }

            if (tag.CompressionFilterSettings != null) {
                tag.CompressionFilterSettings = GetFilterSettings(tag.DataType, tag.CompressionFilterSettings);
            }

            return await _historian.UpdateTag(identity, tagId, tag, cancellationToken).ConfigureAwait(false);
        }


        public async Task<bool> DeleteTag(ClaimsPrincipal identity, string tagIdOrName, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (String.IsNullOrWhiteSpace(tagIdOrName)) {
                throw new ArgumentException("You must specify a tag ID or name.", nameof(tagIdOrName));
            }

            return await _historian.DeleteTag(identity, tagIdOrName, cancellationToken).ConfigureAwait(false);
        }


        public async Task<IEnumerable<StateSet>> GetStateSets(ClaimsPrincipal identity, StateSetFilter filter, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }

            return await _historian.GetStateSets(identity, filter, cancellationToken).ConfigureAwait(false);
        }


        public async Task<StateSet> GetStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (identity == null) {
                throw new ArgumentNullException(nameof(identity));
            }
            if (String.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("You must specify a state set name.", nameof(name));
            }

            return await _historian.GetStateSet(identity, name, cancellationToken).ConfigureAwait(false);
        }


        public async Task<StateSet> CreateStateSet(ClaimsPrincipal identity, string name, IEnumerable<StateSetItem> states, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (String.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("You must specify a state set name.", nameof(name));
            }

            var nonNullStates = states?.Where(x => x != null).ToArray() ?? new StateSetItem[0];

            if (nonNullStates.Length == 0) {
                throw new ArgumentNullException("You must specify at least one state.", nameof(states));
            }

            return await _historian.CreateStateSet(identity, name, nonNullStates, cancellationToken).ConfigureAwait(false);
        }


        public async Task<StateSet> UpdateStateSet(ClaimsPrincipal identity, string name, IEnumerable<StateSetItem> states, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (String.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("You must specify a state set name.", nameof(name));
            }

            var nonNullStates = states?.Where(x => x != null).ToArray() ?? new StateSetItem[0];

            if (nonNullStates.Length == 0) {
                throw new ArgumentNullException("You must specify at least one state.", nameof(states));
            }

            return await _historian.UpdateStateSet(identity, name, nonNullStates, cancellationToken).ConfigureAwait(false);
        }


        public async Task<bool> DeleteStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken) {
            ThrowIfNotReady();

            if (String.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("You must specify a state set name.", nameof(name));
            }

            return await _historian.DeleteStateSet(identity, name, cancellationToken).ConfigureAwait(false);
        }

        #endregion

    }
}
