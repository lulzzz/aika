using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Aika.AspNetCore.Models.Tags;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aika.AspNetCore.Controllers.Configuration {

    /// <summary>
    /// API controller for managing historian tags.
    /// </summary>
    [Route("aika/api/configuration/[controller]")]
    [Authorize(Roles = Aika.Roles.ManageTags)]
    public class TagsController : Controller {

        /// <summary>
        /// Aika historian instance.
        /// </summary>
        private readonly AikaHistorian _historian;


        /// <summary>
        /// Creates a new <see cref="TagsController"/> object.
        /// </summary>
        /// <param name="historian">The Aika historian instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="historian"/> is <see langword="null"/>.</exception>
        public TagsController(AikaHistorian historian) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
        }


        /// <summary>
        /// Gets the number of configured tags.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// Successful responses contain the number of configured tags.  The response content will be 
        /// <see langword="null"/> if the underlying historian chooses to not make this information 
        /// available.
        /// </returns>
        [HttpGet]
        [Route("count")]
        [ProducesResponseType(200, Type = typeof(int?))]
        public async Task<IActionResult> GetTagCount(CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState); // 400
            }

            var identity = (ClaimsIdentity) User.Identity;

            try {
                var result = await _historian.GetTagCount(identity, cancellationToken).ConfigureAwait(false);
                return Ok(result); // 200
            }
            catch (OperationCanceledException) {
                return StatusCode(204); // 204
            }
            catch (SecurityException) {
                return Unauthorized(); // 401
            }
            catch (NotSupportedException) {
                return BadRequest(); // 400
            }
            catch (NotImplementedException) {
                return BadRequest(); // 400
            }
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
        public async Task<IActionResult> GetTags([FromBody] Models.Query.TagSearchRequest request, CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState); // 400
            }

            try {
                var identity = (ClaimsIdentity) User.Identity;
                var result = await _historian.GetTags(identity, request.ToTagDefinitionFilter(), cancellationToken).ConfigureAwait(false);
                return Ok(result.Select(x => new TagDefinitionExtendedDto(x)).ToArray()); // 200
            }
            catch (ArgumentException) {
                return BadRequest(); // 400
            }
            catch (OperationCanceledException) {
                return StatusCode(204); // 204
            }
            catch (SecurityException) {
                return Unauthorized(); // 401
            }
            catch (NotSupportedException) {
                return BadRequest(); // 400
            }
            catch (NotImplementedException) {
                return BadRequest(); // 400
            }
        }


        [HttpGet]
        [Route("{id}", Name = "GetTagById")]
        [ProducesResponseType(200, Type = typeof(TagDefinitionExtendedDto))]
        public async Task<IActionResult> GetTag([FromRoute] string id, CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState); // 400
            }

            var identity = (ClaimsIdentity) User.Identity;

            try {
                var result = await _historian.GetTags(identity, new[] { id }, cancellationToken).ConfigureAwait(false);
                var tag = result.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
                if (tag == null) {
                    return NotFound(id);
                }
                return Ok(new TagDefinitionExtendedDto(tag)); // 200
            }
            catch (ArgumentException) {
                return BadRequest(); // 400
            }
            catch (OperationCanceledException) {
                return StatusCode(204); // 204
            }
            catch (SecurityException) {
                return Unauthorized(); // 401
            }
            catch (NotSupportedException) {
                return BadRequest(); // 400
            }
            catch (NotImplementedException) {
                return BadRequest(); // 400
            }
        }


        [HttpPost]
        [Route("")]
        [ProducesResponseType(201, Type = typeof(TagDefinitionExtendedDto))]
        public async Task<IActionResult> CreateTag([FromBody] TagDefinitionUpdate tag, CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState); // 400
            }

            var identity = (ClaimsIdentity) User.Identity;

            try {
                var result = await _historian.CreateTag(identity, tag, cancellationToken).ConfigureAwait(false);
                return CreatedAtRoute("GetTagById", new { id = result.Id } ,new TagDefinitionExtendedDto(result)); // 200
            }
            catch (ArgumentException) {
                return BadRequest(); // 400
            }
            catch (OperationCanceledException) {
                return StatusCode(204); // 204
            }
            catch (SecurityException) {
                return Unauthorized(); // 401
            }
            catch (NotSupportedException) {
                return BadRequest(); // 400
            }
            catch (NotImplementedException) {
                return BadRequest(); // 400
            }
        }


        [HttpPut]
        [Route("{id}")]
        [ProducesResponseType(200, Type = typeof(TagDefinitionExtendedDto))]
        public async Task<IActionResult> UpdateTag([FromRoute] string id, [FromBody] TagDefinitionUpdate tag, CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState); // 400
            }

            var identity = (ClaimsIdentity) User.Identity;

            try {
                var result = await _historian.UpdateTag(identity, id, tag, cancellationToken).ConfigureAwait(false);
                return Ok(new TagDefinitionExtendedDto(result)); // 200
            }
            catch (ArgumentException) {
                return BadRequest(); // 400
            }
            catch (OperationCanceledException) {
                return StatusCode(204); // 204
            }
            catch (SecurityException) {
                return Unauthorized(); // 401
            }
            catch (NotSupportedException) {
                return BadRequest(); // 400
            }
            catch (NotImplementedException) {
                return BadRequest(); // 400
            }
        }


        [HttpDelete]
        [Route("{id}")]
        [ProducesResponseType(200, Type = typeof(bool))]
        public async Task<IActionResult> DeleteTag([FromRoute] string id, CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState); // 400
            }

            var identity = (ClaimsIdentity) User.Identity;

            try {
                var result = await _historian.DeleteTag(identity, id, cancellationToken).ConfigureAwait(false);
                return Ok(result); // 200
            }
            catch (ArgumentException) {
                return BadRequest(); // 400
            }
            catch (OperationCanceledException) {
                return StatusCode(204); // 204
            }
            catch (SecurityException) {
                return Unauthorized(); // 401
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
