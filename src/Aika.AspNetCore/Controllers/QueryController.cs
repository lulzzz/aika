﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Aika.AspNetCore.Models;
using Aika.AspNetCore.Models.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aika.AspNetCore.Controllers {

    /// <summary>
    /// API controller for performing tag data queries.
    /// </summary>
    [Route("aika/api/[controller]")]
    [Authorize(Roles = Aika.Roles.ReadTagData)]
    public class QueryController : Controller {

        /// <summary>
        /// Aika historian instance.
        /// </summary>
        private readonly AikaHistorian _historian;


        /// <summary>
        /// Creates a new <see cref="QueryController"/> object.
        /// </summary>
        /// <param name="historian">The Aika historian instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="historian"/> is <see langword="null"/>.</exception>
        public QueryController(AikaHistorian historian) {
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
        [Route("tags")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<TagDefinitionDto>))]
        public async Task<IActionResult> GetTags([FromBody] TagSearchRequest request, CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState); // 400
            }

            var identity = (ClaimsIdentity) User.Identity;

            try {
                var result = await _historian.GetTags(identity, request.ToTagDefinitionFilter(), cancellationToken).ConfigureAwait(false);
                return Ok(result.Select(x => new TagDefinitionDto(x)).ToArray()); // 200
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
        /// Gets the available state set definitions (i.e. the sets of named states that discrete, state-based tags can use).
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// The content of a successful response will be the defined state sets, indexed by set name.
        /// </returns>
        [HttpGet]
        [Route("tags/statesets")]
        [ProducesResponseType(200, Type = typeof(IDictionary<string, StateSetDto>))]
        public async Task<IActionResult> GetStateSets(CancellationToken cancellationToken) {
            var identity = (ClaimsIdentity) User.Identity;

            try {
                var result = await _historian.GetStateSets(identity, cancellationToken).ConfigureAwait(false);
                return Ok(result.Values.OrderBy(x => x.Name).ToDictionary(x => x.Name, x => new StateSetDto(x))); // 200
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
        [Route("tags/statesets/{name}")]
        [ProducesResponseType(200, Type = typeof(StateSetDto))]
        public async Task<IActionResult> GetStateSet([FromRoute] string name, CancellationToken cancellationToken) {
            var identity = (ClaimsIdentity) User.Identity;

            try {
                var result = await _historian.GetStateSet(identity, name, cancellationToken).ConfigureAwait(false);
                if (result == null) {
                    return NotFound(); // 404
                }
                return Ok(new StateSetDto(result)); // 200
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
        /// Performs a snapshot data query.
        /// </summary>
        /// <param name="request">The snapshot request.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// Successful responses contain a dictionary that maps from tag name to snapshot value.
        /// </returns>
        [HttpPost]
        [Route("tags/snapshot")]
        [ProducesResponseType(200, Type = typeof(IDictionary<string, TagValueDto>))]
        public async Task<IActionResult> GetSnapshotData([FromBody] SnapshotDataRequest request, CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState); // 400
            }

            var identity = (ClaimsIdentity) User.Identity;

            try {
                var result = await _historian.ReadSnapshotData(identity, request.Tags, cancellationToken).ConfigureAwait(false);
                return Ok(result.ToDictionary(x => x.Key, x => new TagValueDto(x.Value))); // 200
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
        /// Performs a snapshot data query.
        /// </summary>
        /// <param name="tag">The tag names to query.</param>
        /// <param name="cancellationToken">The cancellation token for the response.</param>
        /// <returns>
        /// Successful responses contain a dictionary that maps from tag name to snapshot value.
        /// </returns>
        [HttpGet]
        [Route("tags/snapshot")]
        [ProducesResponseType(200, Type = typeof(IDictionary<string, TagValueDto>))]
        public Task<IActionResult> GetSnapshotData([FromQuery] string[] tag, CancellationToken cancellationToken) {
            var model = new SnapshotDataRequest() {
                Tags = tag
            };

            TryValidateModel(model);
            return GetSnapshotData(model, cancellationToken);
        }


        /// <summary>
        /// Performs a raw, unprocessed data query.
        /// </summary>
        /// <param name="request">The raw data request.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// Successful responses contain a dictionary that maps from tag name to historical tag values.
        /// </returns>
        [HttpPost]
        [Route("tags/raw")]
        [ProducesResponseType(200, Type = typeof(IDictionary<string, HistoricalTagValuesDto>))]
        public async Task<IActionResult> GetRawData([FromBody] RawDataRequest request, CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState); // 400
            }

            var identity = (ClaimsIdentity) User.Identity;

            try {
                var result = await _historian.ReadRawData(identity, request.Tags, request.Start, request.End, request.PointCount, cancellationToken).ConfigureAwait(false);
                return Ok(result.ToDictionary(x => x.Key, x => new HistoricalTagValuesDto(x.Value))); // 200
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
        /// Performs a raw, unprocessed data query.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the response.</param>
        /// <param name="tag">The tag names to query.</param>
        /// <param name="start">The UTC start time for the query.</param>
        /// <param name="end">The UTC end time for the query.</param>
        /// <param name="pointCount">The point count to use for the query.</param>
        /// <returns>
        /// Successful responses contain a dictionary that maps from tag name to historical tag values.
        /// </returns>
        [HttpGet]
        [Route("tags/processed")]
        [ProducesResponseType(200, Type = typeof(IDictionary<string, HistoricalTagValuesDto>))]
        public Task<IActionResult> GetRawData([FromQuery] string[] tag, [FromQuery] DateTime start, [FromQuery] DateTime end, [FromQuery] int pointCount, CancellationToken cancellationToken) {
            var model = new RawDataRequest() {
                Tags = tag,
                Start = start,
                End = end,
                PointCount = pointCount
            };

            TryValidateModel(model);
            return GetRawData(model, cancellationToken);
        }


        /// <summary>
        /// Performs an aggregated data query.
        /// </summary>
        /// <param name="request">The historical data request.</param>
        /// <param name="cancellationToken">The cancellation token for the response.</param>
        /// <returns>
        /// Successful responses contain a dictionary that maps from tag name to historical tag values.
        /// </returns>
        [HttpPost]
        [Route("tags/processed")]
        [ProducesResponseType(200, Type = typeof(IDictionary<string, HistoricalTagValuesDto>))]
        public async Task<IActionResult> GetProcessedData([FromBody] AggregatedDataRequest request, CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState); // 400
            }

            var identity = (ClaimsIdentity) User.Identity;

            try {
                var result = request.SampleInterval.HasValue
                    ? await _historian.ReadProcessedData(identity, request.Tags, request.Function, request.Start, request.End, request.SampleInterval.Value, cancellationToken).ConfigureAwait(false)
                    : await _historian.ReadProcessedData(identity, request.Tags, request.Function, request.Start, request.End, request.PointCount.Value, cancellationToken).ConfigureAwait(false);
                return Ok(result.ToDictionary(x => x.Key, x => new HistoricalTagValuesDto(x.Value))); // 200
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
        /// Performs an aggregated data query.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the response.</param>
        /// <param name="tag">The tag names to query.</param>
        /// <param name="function">The data function to use.</param>
        /// <param name="start">The UTC start time for the query.</param>
        /// <param name="end">The UTC end time for the query.</param>
        /// <param name="sampleInterval">The sample interval to use for aggregation.  Specify a value for this *or* <paramref name="pointCount"/>, but not both.</param>
        /// <param name="pointCount">The point count to use for the query.  Specify a value for this *or* <paramref name="sampleInterval"/>, but not both.</param>
        /// <returns>
        /// Successful responses contain a dictionary that maps from tag name to historical tag values.
        /// </returns>
        [HttpGet]
        [Route("tags/processed")]
        [ProducesResponseType(200, Type = typeof(IDictionary<string, HistoricalTagValuesDto>))]
        public Task<IActionResult> GetProcessedData(CancellationToken cancellationToken, [FromQuery] string[] tag, [FromQuery] string function, [FromQuery] DateTime start, [FromQuery] DateTime end, [FromQuery] TimeSpan? sampleInterval = null, [FromQuery] int? pointCount = null) {
            var model = new AggregatedDataRequest() {
                Tags = tag,
                Function = function,
                Start = start,
                End = end,
                SampleInterval = sampleInterval,
                PointCount = pointCount
            };

            TryValidateModel(model);
            return GetProcessedData(model, cancellationToken);
        }

    }
}
