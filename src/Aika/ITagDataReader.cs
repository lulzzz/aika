using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aika {

    /// <summary>
    /// Describes a service for reading data from the historian.
    /// </summary>
    public interface ITagDataReader {

        /// <summary>
        /// When implemented in a derived type, tests if the calling identity is allowed to read data from the specified tag names.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The tag names.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary that maps from tag name to authorization result.
        /// </returns>
        Task<IDictionary<string, bool>> CanReadTagData(ClaimsIdentity identity, IEnumerable<string> tagNames, CancellationToken cancellationToken);


        /// <summary>
        /// When implemented in a derived type, gets the data query functions supported by the historian.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A collection of data query functions.  Common functions are defined in the <see cref="DataQueryFunction"/> class.
        /// </returns>
        /// <remarks>
        /// If the <see cref="ITagDataReader"/> used by an <see cref="AikaHistorian"/> returns <see langword="false"/> for a given function name, 
        /// </remarks>
        Task<IEnumerable<DataQueryFunction>> GetAvailableDataQueryFunctions(CancellationToken cancellationToken);


        /// <summary>
        /// When implemented in a derived type, retrieves snapshot data from the historian.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The names of the tags to query.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary of snapshot values, indexed by tag name.
        /// </returns>
        Task<IDictionary<string, TagValue>> ReadSnapshotData(ClaimsIdentity identity, IEnumerable<string> tagNames, CancellationToken cancellationToken);


        /// <summary>
        /// When implemented in a derived type, performs a historical data query on the historian.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The names of the tags to query.</param>
        /// <param name="dataFunction">The data function specifying the type of aggregation to perform on the data.  See <see cref="DataQueryFunction"/> for common function names.</param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="sampleInterval">The sample interval to use for aggregation.  The exact interpretation of this parameter depends on the <paramref name="dataFunction"/> that is used.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary of historical data, indexed by tag name.
        /// </returns>
        /// <seealso cref="DataQueryFunction"/>
        Task<IDictionary<string, TagValueCollection>> ReadHistoricalData(ClaimsIdentity identity, IEnumerable<string> tagNames, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, TimeSpan sampleInterval, CancellationToken cancellationToken);


        /// <summary>
        /// When implemented in a derived type, performs a historical data query on the historian.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The names of the tags to query.</param>
        /// <param name="dataFunction">The data function specifying the type of aggregation to perform on the data.  See <see cref="DataQueryFunction"/> for common function names.</param>
        /// <param name="utcStartTime">The UTC start time for the query.</param>
        /// <param name="utcEndTime">The UTC end time for the query.</param>
        /// <param name="pointCount">The number of samples to return.  The exact interpretation of this parameter depends on the <paramref name="dataFunction"/> that is used.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary of historical data, indexed by tag name.
        /// </returns>
        /// <seealso cref="DataQueryFunction"/>
        Task<IDictionary<string, TagValueCollection>> ReadHistoricalData(ClaimsIdentity identity, IEnumerable<string> tagNames, string dataFunction, DateTime utcStartTime, DateTime utcEndTime, int pointCount, CancellationToken cancellationToken);

    }
}
