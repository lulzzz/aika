using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika.Client.Dto;

namespace Aika.Client.Clients {

    /// <summary>
    /// Describes a client for querying the Aika API's tag configuration and state set configuration endpoints.
    /// </summary>
    public interface ITagConfigurationClient {

        /// <summary>
        /// Performs a tag search and returns extended information about the matching tags.  Authorized 
        /// using the <c>aika:managetags</c> authorization policy.
        /// </summary>
        /// <param name="filter">The tag search filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the matching tag definitions.
        /// </returns>
        Task<IEnumerable<TagDefinitionExtendedDto>> GetTags(TagSearchRequest filter, CancellationToken cancellationToken);


        /// <summary>
        /// Gets the extended definition for the tag with the specified ID.  Authorized using the 
        /// <c>aika:managetags</c> authorization policy.
        /// </summary>
        /// <param name="tagId">The tag ID.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the matching tag definition.
        /// </returns>
        Task<TagDefinitionExtendedDto> GetTag(string tagId, CancellationToken cancellationToken);


        /// <summary>
        /// Creates a new tag in the historian.  Authorized using the <c>aika:managetags</c> 
        /// authorization policy.
        /// </summary>
        /// <param name="tagDefinition">The tag definition to create.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the extended definition for the new tag.
        /// </returns>
        Task<TagDefinitionExtendedDto> CreateTag(TagDefinitionUpdateDto tagDefinition, CancellationToken cancellationToken);


        /// <summary>
        /// Updates an existing historian tag.  Authorized using the <c>aika:managetags</c> 
        /// authorization policy.
        /// </summary>
        /// <param name="tagId">The ID of the tag to update.</param>
        /// <param name="tagDefinition">The updated tag definition.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the extended definition for the updated tag.
        /// </returns>
        Task<TagDefinitionExtendedDto> UpdateTag(string tagId, TagDefinitionUpdateDto tagDefinition, CancellationToken cancellationToken);


        /// <summary>
        /// Deletes a historian tag.  Authorized using the <c>aika:managetags</c> authorization policy.
        /// </summary>
        /// <param name="tagId">The ID of the tag to delete.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return a flag indicating if the delete was successful.
        /// </returns>
        Task<bool> DeleteTag(string tagId, CancellationToken cancellationToken);


        /// <summary>
        /// Gets the available tag state sets.  State-based tags specify a state set that controls the 
        /// possible values for the tag.  Authorized using the <c>aika:managetags</c> authorization 
        /// policy.
        /// </summary>
        /// <param name="filter">The state set search filter.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the matching state sets.
        /// </returns>
        Task<IEnumerable<StateSetDto>> GetStateSets(StateSetSearchRequest filter, CancellationToken cancellationToken);


        /// <summary>
        /// Gets the state set with the specified name.  State-based tags specify a state set that 
        /// controls the possible values for the tag.  Authorized using the <c>aika:managetags</c> 
        /// authorization policy.
        /// </summary>
        /// <param name="name">The state set name.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the matching state set.
        /// </returns>
        Task<StateSetDto> GetStateSet(string name, CancellationToken cancellationToken);


        /// <summary>
        /// Creates a new tag state set.  Authorized using the <c>aika:managetags</c> authorization 
        /// policy.
        /// </summary>
        /// <param name="stateSet">The state set definition.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the new state set definition.
        /// </returns>
        Task<StateSetDto> CreateStateSet(StateSetDto stateSet, CancellationToken cancellationToken);


        /// <summary>
        /// Updates a tag state set.  Authorized using the <c>aika:managetags</c> authorization 
        /// policy.
        /// </summary>
        /// <param name="stateSet">The updated state set.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the updated state set.
        /// </returns>
        Task<StateSetDto> UpdateStateSet(StateSetDto stateSet, CancellationToken cancellationToken);


        /// <summary>
        /// Deletes a tag state set.  Authorized using the <c>aika:managetags</c> authorization 
        /// policy.
        /// </summary>
        /// <param name="name">The name of the state set to delete.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return a flag indicating if the delete was successful.
        /// </returns>
        Task<bool> DeleteStateSet(string name, CancellationToken cancellationToken);

    }
}
