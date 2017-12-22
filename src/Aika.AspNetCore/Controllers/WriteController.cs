using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Aika.AspNetCore.Models.Write;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aika.AspNetCore.Controllers {

    /// <summary>
    /// API controller for performing operations on the Aika data stream.
    /// </summary>
    [Route("aika/api/[controller]")]
    [Authorize(Roles = Aika.Roles.WriteTagData)]
    public class WriteController : Controller {

        /// <summary>
        /// The Aika historian instance.
        /// </summary>
        private readonly AikaHistorian _historian;


        /// <summary>
        /// Creates a new <see cref="WriteController"/> object.
        /// </summary>
        /// <param name="historian">The Aika historian instance.</param>
        public WriteController(AikaHistorian historian) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
        }


        /// <summary>
        /// Writes data to the historian snapshot.
        /// </summary>
        /// <param name="request">The data to write.  Values will only be updated in the historian if they pass the destination tags' exception and compression filters.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// Successful responses contain a write summary for each tag in the request.
        /// </returns>
        [HttpPost]
        [Route("")]
        [ProducesResponseType(200, Type = typeof(IDictionary<string, WriteTagValuesResultDto>))]
        public async Task<IActionResult> WriteSnapshotData([FromBody] WriteTagValuesRequest request, CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState); // 400
            }

            var identity = (ClaimsIdentity) User.Identity;

            try {
                var result = await _historian.WriteTagData(identity, request.ToTagValueDictionary(), cancellationToken).ConfigureAwait(false);
                return Ok(result.ToDictionary(x => x.Key, x => new WriteTagValuesResultDto(x.Value))); // 200
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
        /// Writes data directly into the historian archive.
        /// </summary>
        /// <param name="request">The data to write.  Values are inserted directly into the archive, bypassing the exception and compression filters.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// Successful responses contain a write summary for each tag in the request.
        /// </returns>
        [HttpPost]
        [Route("archive")]
        [ProducesResponseType(200, Type = typeof(IDictionary<string, WriteTagValuesResultDto>))]
        public async Task<IActionResult> WriteArchiveData([FromBody] WriteTagValuesRequest request, CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState); // 400
            }

            var identity = (ClaimsIdentity) User.Identity;

            try {
                var result = await _historian.InsertTagData(identity, request.ToTagValueDictionary(), cancellationToken).ConfigureAwait(false);
                return Ok(result.ToDictionary(x => x.Key, x => new WriteTagValuesResultDto(x.Value))); // 200
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
