using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aika {

    /// <summary>
    /// Describes methods for performing tag searches on a historian.
    /// </summary>
    public interface ITagSearch {

        /// <summary>
        /// When implemented in a derived type, performs a tag search.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="filter">The tag search filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A collection of matching tags.
        /// </returns>
        Task<IEnumerable<TagDefinition>> GetTags(ClaimsPrincipal identity, TagDefinitionFilter filter, CancellationToken cancellationToken);


        /// <summary>
        /// When implemented in a derived type, gets the definitions for the specified tags.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagIdsOrNames">The names of the tags to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The definitions of the requested tags.
        /// </returns>
        Task<IEnumerable<TagDefinition>> GetTags(ClaimsPrincipal identity, IEnumerable<string> tagIdsOrNames, CancellationToken cancellationToken);


        /// <summary>
        /// When implemented in a derived type, gets the defined state sets.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The state sets defined by the historian, indexed by set name.
        /// </returns>
        Task<IDictionary<string, StateSet>> GetStateSets(ClaimsPrincipal identity, CancellationToken cancellationToken);


        /// <summary>
        /// When implemented in a derived type, gets the specified state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="name">The name of the state set.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The corresponding <see cref="StateSet"/>.
        /// </returns>
        Task<StateSet> GetStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken);

    }
}
