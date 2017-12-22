using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aika {

    /// <summary>
    /// Describes a service for writing data to the historian.
    /// </summary>
    public interface ITagDataWriter {

        /// <summary>
        /// When implemented in a derived type, tests if the calling identity is allowed to write data to the specified tag names.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagNames">The tag names.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A dictionary that maps from tag name to authorization result.
        /// </returns>
        Task<IDictionary<string, bool>> CanWriteTagData(ClaimsIdentity identity, IEnumerable<string> tagNames, CancellationToken cancellationToken);

    }
}
