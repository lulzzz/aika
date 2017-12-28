using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Aika.Client.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aika.AspNetCore.Controllers.Configuration {

    /// <summary>
    /// API controller for managing the state sets available to historian tags.
    /// </summary>
    [Route("aika/api/configuration/[controller]")]
    [Authorize(Policy = Authorization.Policies.ManageTags)]
    public class StateSetsController : Controller {

        /// <summary>
        /// Aika historian instance.
        /// </summary>
        private readonly AikaHistorian _historian;


        /// <summary>
        /// Creates a new <see cref="StateSetsController"/> object.
        /// </summary>
        /// <param name="historian">The Aika historian instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="historian"/> is <see langword="null"/>.</exception>
        public StateSetsController(AikaHistorian historian) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
        }


        /// <summary>
        /// Gets the available state set definitions (i.e. the sets of named states that discrete, state-based tags can use).
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The content of a successful response will be the defined state sets, indexed by set name.
        /// </returns>
        [HttpGet]
        [Route("")]
        [ProducesResponseType(200, Type = typeof(IDictionary<string, StateSetDto>))]
        public async Task<IActionResult> GetStateSets(CancellationToken cancellationToken) {
            try {
                var result = await _historian.GetStateSets(User, cancellationToken).ConfigureAwait(false);
                return Ok(result.Values.OrderBy(x => x.Name).ToDictionary(x => x.Name, x => x.ToStateSetDto())); // 200
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


        /// <summary>
        /// Gets the specified state set definition.
        /// </summary>
        /// <param name="name">The name of the state set.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The content of a successful response will be the requested state set definition.
        /// </returns>
        [HttpGet]
        [Route("{name}", Name = "GetStateSetByName")]
        [ProducesResponseType(200, Type = typeof(StateSetDto))]
        public async Task<IActionResult> GetStateSet([FromRoute] string name, CancellationToken cancellationToken) {
            try {
                var result = await _historian.GetStateSet(User, name, cancellationToken).ConfigureAwait(false);
                if (result == null) {
                    return NotFound(); // 404
                }
                return Ok(result.ToStateSetDto()); // 200
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


        /// <summary>
        /// Creates a state set for use with state-based tags.
        /// </summary>
        /// <param name="stateSet">The state set definition.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The content of a successful response is the newly-created state set.
        /// </returns>
        [HttpPost]
        [Route("")]
        [ProducesResponseType(201, Type = typeof(StateSetDto))]
        public async Task<IActionResult> CreateStateSet([FromBody] StateSetDto stateSet, CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState); // 400
            }

            try {
                var result = await _historian.CreateStateSet(User, stateSet.Name, stateSet.States.Where(x => x != null).Select(x => x.ToStateSetItem()).ToArray(), cancellationToken).ConfigureAwait(false);
                return CreatedAtRoute("GetStateSetByName", new { name = result.Name }, result); // 201
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


        /// <summary>
        /// Updates an existing state set.
        /// </summary>
        /// <param name="stateSet">The updated state set definition.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The content of a successful response will be the updated state set definition.
        /// </returns>
        [HttpPut]
        [Route("")]
        [ProducesResponseType(200, Type = typeof(StateSetDto))]
        public async Task<IActionResult> UpdateStateSet([FromBody] StateSetDto stateSet, CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState); // 400
            }
            
            try {
                var result = await _historian.UpdateStateSet(User, stateSet.Name, stateSet.States?.Where(x => x != null).Select(x => x.ToStateSetItem()).ToArray(), cancellationToken).ConfigureAwait(false);
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


        /// <summary>
        /// Deletes a state set.
        /// </summary>
        /// <param name="name">The name of the state set to delete.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The content of a successful response will be a flag indicating if the delete was successful.
        /// </returns>
        [HttpDelete]
        [Route("{name}")]
        [ProducesResponseType(200, Type = typeof(bool))]
        public async Task<IActionResult> DeleteStateSet([FromRoute] string name, CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState); // 400
            }
            
            try {
                var result = await _historian.DeleteStateSet(User, name, cancellationToken).ConfigureAwait(false);
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
