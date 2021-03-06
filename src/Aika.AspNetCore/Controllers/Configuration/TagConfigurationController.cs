﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Aika.Client.Dto;
using Aika.Tags;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aika.AspNetCore.Controllers.Configuration {

    /// <summary>
    /// API controller for managing historian tags.
    /// </summary>
    [Route("aika/api/configuration/tags")]
    [Authorize(Policy = Authorization.Policies.ManageTags)]
    public class TagConfigurationController : Controller {

        /// <summary>
        /// Aika historian instance.
        /// </summary>
        private readonly AikaHistorian _historian;


        /// <summary>
        /// Creates a new <see cref="TagConfigurationController"/> object.
        /// </summary>
        /// <param name="historian">The Aika historian instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="historian"/> is <see langword="null"/>.</exception>
        public TagConfigurationController(AikaHistorian historian) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
        }


        /// <summary>
        /// Performs a tag search.
        /// </summary>
        /// <param name="request">The tag search request.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// Successful responses contain a page of matching search results.
        /// </returns>
        [HttpPost]
        [Route("search")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<TagDefinitionExtendedDto>))]
        public async Task<IActionResult> GetTags([FromBody] TagSearchRequest request, CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState); // 400
            }

            try {
                var result = await _historian.FindTags(User, request.ToTagDefinitionFilter(), cancellationToken).ConfigureAwait(false);
                return Ok(result.Select(x => x.ToTagDefinitionExtendedDto()).ToArray()); // 200
            }
            catch (ArgumentException) {
                return BadRequest(); // 400
            }
            catch (OperationCanceledException) {
                return StatusCode(204); // 204
            }
            catch (SecurityException) {
                return Forbid(); // 403
            }
            catch (NotSupportedException) {
                return BadRequest(); // 400
            }
            catch (NotImplementedException) {
                return BadRequest(); // 400
            }
        }


        /// <summary>
        /// Gets the extended tag definition for the specified ID.
        /// </summary>
        /// <param name="id">The tag ID.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// Successful responses contain the extended definition for the tag.
        /// </returns>
        [HttpGet]
        [Route("{id}", Name = "GetTagById")]
        [ProducesResponseType(200, Type = typeof(TagDefinitionExtendedDto))]
        public async Task<IActionResult> GetTag([FromRoute] string id, CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState); // 400
            }

            try {
                var result = await _historian.GetTags(User, new[] { id }, cancellationToken).ConfigureAwait(false);
                if (!result.TryGetValue(id, out var tag)) {

                }
                if (tag == null) {
                    return NotFound(id);
                }
                return Ok(tag.ToTagDefinitionExtendedDto()); // 200
            }
            catch (ArgumentException) {
                return BadRequest(); // 400
            }
            catch (OperationCanceledException) {
                return StatusCode(204); // 204
            }
            catch (SecurityException) {
                return Forbid(); // 403
            }
            catch (NotSupportedException) {
                return BadRequest(); // 400
            }
            catch (NotImplementedException) {
                return BadRequest(); // 400
            }
        }


        /// <summary>
        /// Creates a new tag.
        /// </summary>
        /// <param name="tag">The tag definition.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// Successful responses contain the new tag definition.
        /// </returns>
        [HttpPost]
        [Route("")]
        [ProducesResponseType(201, Type = typeof(TagDefinitionExtendedDto))]
        public async Task<IActionResult> CreateTag([FromBody] TagSettings tag, CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState); // 400
            }

            try {
                var result = await _historian.CreateTag(User, tag, cancellationToken).ConfigureAwait(false);
                return CreatedAtRoute("GetTagById", new { id = result.Id }, result.ToTagDefinitionExtendedDto()); // 201
            }
            catch (ArgumentException) {
                return BadRequest(); // 400
            }
            catch (OperationCanceledException) {
                return StatusCode(204); // 204
            }
            catch (SecurityException) {
                return Forbid(); // 403
            }
            catch (NotSupportedException) {
                return BadRequest(); // 400
            }
            catch (NotImplementedException) {
                return BadRequest(); // 400
            }
        }


        /// <summary>
        /// Updates an existing tag.
        /// </summary>
        /// <param name="id">The ID of the tag to update.</param>
        /// <param name="tag">The updated tag definition.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// Successful responses contain the updated tag definition.
        /// </returns>
        [HttpPut]
        [Route("{id}")]
        [ProducesResponseType(200, Type = typeof(TagDefinitionExtendedDto))]
        public async Task<IActionResult> UpdateTag([FromRoute] string id, [FromBody] TagDefinitionUpdateDto tag, CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState); // 400
            }

            try {
                var result = await _historian.UpdateTag(User, id, tag.ToTagDefinitionUpdate(), tag.ChangeDescription, cancellationToken).ConfigureAwait(false);
                return Ok(result.ToTagDefinitionExtendedDto()); // 200
            }
            catch (ArgumentException) {
                return BadRequest(); // 400
            }
            catch (OperationCanceledException) {
                return StatusCode(204); // 204
            }
            catch (SecurityException) {
                return Forbid(); // 403
            }
            catch (NotSupportedException) {
                return BadRequest(); // 400
            }
            catch (NotImplementedException) {
                return BadRequest(); // 400
            }
        }
        

        /// <summary>
        /// Deletes a tag.
        /// </summary>
        /// <param name="id">The ID of the tag to delete.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// 
        /// </returns>
        [HttpDelete]
        [Route("{id}")]
        [ProducesResponseType(200, Type = typeof(bool))]
        public async Task<IActionResult> DeleteTag([FromRoute] string id, CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState); // 400
            }

            try {
                var result = await _historian.DeleteTag(User, id, cancellationToken).ConfigureAwait(false);
                return Ok(result); // 200
            }
            catch (ArgumentException) {
                return BadRequest(); // 400
            }
            catch (OperationCanceledException) {
                return StatusCode(204); // 204
            }
            catch (SecurityException) {
                return Forbid(); // 403
            }
            catch (NotSupportedException) {
                return BadRequest(); // 400
            }
            catch (NotImplementedException) {
                return BadRequest(); // 400
            }
        }

    }

}
