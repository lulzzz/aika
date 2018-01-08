using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aika {
    /// <summary>
    /// Interface for managing tag definitions.
    /// </summary>
    public interface ITagManager {

        /// <summary>
        /// Gets the total number of configured tags.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The total tag count.  Implementations can return <see langword="null"/> if they do not 
        /// track this number, or if it is impractical to calculate it.
        /// </returns>
        Task<int?> GetTagCount(ClaimsPrincipal identity, CancellationToken cancellationToken);


        /// <summary>
        /// Creates a new tag.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tag">The tag definition.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The new tag definition.
        /// </returns>
        Task<TagDefinition> CreateTag(ClaimsPrincipal identity, TagSettings tag, CancellationToken cancellationToken);


        /// <summary>
        /// Updates a tag.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagId">The ID of the tag to update.</param>
        /// <param name="update">The updated tag definition.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The updated tag definition.
        /// </returns>
        /// <remarks>
        /// Implementers should call <see cref="TagDefinition.Update(TagSettings)"/> to update the target tag.
        /// </remarks>
        Task<TagDefinition> UpdateTag(ClaimsPrincipal identity, string tagId, TagSettings update, CancellationToken cancellationToken);


        /// <summary>
        /// Deletes a tag.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="tagId">The ID of the tag to delete.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A flag that indicates if the tag was deleted.
        /// </returns>
        /// <remarks>
        /// Implementations should return <see langword="false"/> only if the specified 
        /// <paramref name="tagId"/> does not exist.  Delete operations that fail due to 
        /// authorization issues should throw a <see cref="System.Security.SecurityException"/>.
        /// </remarks>
        Task<bool> DeleteTag(ClaimsPrincipal identity, string tagId, CancellationToken cancellationToken);


        /// <summary>
        /// Creates a new state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="settings">The state set settings.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A new <see cref="StateSet"/>.
        /// </returns>
        Task<StateSet> CreateStateSet(ClaimsPrincipal identity, StateSetSettings settings, CancellationToken cancellationToken);


        /// <summary>
        /// Updates an existing state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="name">The name of the state set.</param>
        /// <param name="settings">The state set settings.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The updated <see cref="StateSet"/>.
        /// </returns>
        Task<StateSet> UpdateStateSet(ClaimsPrincipal identity, string name, StateSetSettings settings, CancellationToken cancellationToken);


        /// <summary>
        /// Deletes the specified state set.
        /// </summary>
        /// <param name="identity">The identity of the caller.</param>
        /// <param name="name">The name of the state set.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A flag that indicates if the state set was deleted.
        /// </returns>
        /// <remarks>
        /// Implementations should return <see langword="false"/> only if the specified 
        /// state set <paramref name="name"/> does not exist.  Delete operations that fail due to 
        /// authorization issues should throw a <see cref="System.Security.SecurityException"/>.
        /// </remarks>
        Task<bool> DeleteStateSet(ClaimsPrincipal identity, string name, CancellationToken cancellationToken);

    }
}
