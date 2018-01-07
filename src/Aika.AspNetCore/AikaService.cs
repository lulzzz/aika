using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aika.AspNetCore {
    /// <summary>
    /// <see cref="HostedService"/> that initializes Aika in the background.
    /// </summary>
    public class AikaService : HostedService {

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger<AikaService> _logger;

        /// <summary>
        /// The service options.
        /// </summary>
        private readonly AikaServiceOptions _options;

        /// <summary>
        /// The Aika historian instance.
        /// </summary>
        private readonly AikaHistorian _aika;


        /// <summary>
        /// Creates a new <see cref="AikaService"/> object.
        /// </summary>
        /// <param name="aika">The <see cref="AikaHistorian"/> to initialize.</param>
        /// <param name="options">The service options.</param>
        /// <param name="logger">The logger for the service.</param>
        /// <exception cref="ArgumentNullException"><paramref name="aika"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
        internal AikaService(AikaHistorian aika, AikaServiceOptions options, ILogger<AikaService> logger) {
            _aika = aika ?? throw new ArgumentNullException(nameof(aika));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }


        /// <summary>
        /// Initializes the Aika historian.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel initialization.</param>
        /// <returns>
        /// A task that will initialize the historian.
        /// </returns>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
            if (_options.OnBeforeInit != null) {
                if (_logger?.IsEnabled(LogLevel.Trace) ?? false) {
                    _logger.LogTrace("Start: BeforeInit");
                }
                try {
                    await _options.OnBeforeInit(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    throw;
                }
                catch (Exception e) {
                    _logger?.LogError("An error occurred during the pre-init handler.", e);
                    throw;
                }
                finally {
                    if (_logger?.IsEnabled(LogLevel.Trace) ?? false) {
                        _logger.LogTrace("End: BeforeInit");
                    }
                }
            }

            try {
                if (_logger?.IsEnabled(LogLevel.Trace) ?? false) {
                    _logger.LogTrace("Start: Init");
                }

                await _aika.Init(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                _logger?.LogError("An error occurred while initializing the Aika historian.", e);
                throw;
            }
            finally {
                if (_logger?.IsEnabled(LogLevel.Trace) ?? false) {
                    _logger.LogTrace("End: Init");
                }
            }

            if (_options.OnAfterInit != null) {
                if (_logger?.IsEnabled(LogLevel.Trace) ?? false) {
                    _logger.LogTrace("Start: AfterInit");
                }
                try {
                    await _options.OnAfterInit(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    throw;
                }
                catch (Exception e) {
                    _logger?.LogError("An error occurred during the post-init handler.", e);
                    throw;
                }
                finally {
                    if (_logger?.IsEnabled(LogLevel.Trace) ?? false) {
                        _logger.LogTrace("End: AfterInit");
                    }
                }
            }
        }

    }
}
