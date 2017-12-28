using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aika.Client.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aika.AspNetCore.Controllers {

    /// <summary>
    /// API controller that provides runtime information about the Aika historian.
    /// </summary>
    [Route("aika/api/[controller]")]
    [Authorize]
    public class InfoController : Controller {

        /// <summary>
        /// Aika historian instance.
        /// </summary>
        private readonly AikaHistorian _historian;


        /// <summary>
        /// Creates a new <see cref="InfoController"/> object.
        /// </summary>
        /// <param name="historian">The Aika historian instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="historian"/> is <see langword="null"/>.</exception>
        public InfoController(AikaHistorian historian) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
        }


        /// <summary>
        /// Gets information about the Aika historian.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// Successful responses contain information about the Aika historian.
        /// </returns>
        [HttpGet]
        [Route("")]
        [ProducesResponseType(200, Type = typeof(AikaInfoDto))]
        public async Task<IActionResult> GetInfo(CancellationToken cancellationToken) {
            var result = new AikaInfoDto() {
                UtcStartupTime = _historian.UtcStartupTime,
                Version = _historian.GetType().Assembly?.GetName().Version.ToString(),
                Properties = new Dictionary<string, string>()
            };

            return Ok(result); // 200
        }


        /// <summary>
        /// Gets extended information about the Aika historian and its underlying historian implementation.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// Successful responses contain information about the Aika historian and its underlying historian implementation.
        /// </returns>
        [HttpGet]
        [Route("extended")]
        [Authorize(Policy = Authorization.Policies.Administrator)]
        [ProducesResponseType(200, Type = typeof(AikaInfoDto))]
        public async Task<IActionResult> GetInfoExtended(CancellationToken cancellationToken) {
            var result = new AikaInfoExtendedDto() {
                UtcStartupTime = _historian.UtcStartupTime,
                Version = _historian.GetType().Assembly?.GetName().Version.ToString(),
                Properties = new Dictionary<string, string>(),
                Historian = await _historian.ToHistorianInfoDto(cancellationToken).ConfigureAwait(false)
            };

            return Ok(result); // 200
        }

    }
}
