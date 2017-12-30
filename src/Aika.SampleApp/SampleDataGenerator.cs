using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aika.AspNetCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aika.SampleApp {
    /// <summary>
    /// Background service that configures two historian tags (named <c>Sinusoid</c> and 
    /// <c>Sinusoid_Unfiltered</c> respectively) and populates the tags with sinusoid wave data 
    /// starting from one day prior to 
    /// startup time.
    /// </summary>
    /// <remarks>
    /// 
    /// <para>
    /// This service can only be used with the <see cref="Aika.Historians.InMemoryHistorian"/> 
    /// implementation.
    /// </para>
    /// 
    /// <para>
    /// Both tags contain data for a sinusoid wave with a 12 hour period and an amplitude of 50 (i.e. 
    /// values range from -50 to 50).  A new sample is generated for both tags every 30 seconds.
    /// </para>
    /// 
    /// <para>
    /// The <c>Sinusoid</c> tag is configured to use both exception and compression filtering, to 
    /// demonstrate how these techniques can be used to record only meaningful data points to the 
    /// historian archive, while maintaining the shape of the data.  The tag uses an absolute 
    /// exception deviation value of 0.5 (i.e. an incoming value will be rejected by the filter 
    /// unless it differs from the previous value to pass through the filter by more than 0.5).  
    /// </para>
    /// 
    /// <para>
    /// Values that pass through the exception filter will be passed to the compression filter.  
    /// The tag's compression deviation is 0.75 (i.e. 1.5x the exception deviation).  The 
    /// compression deviation, combined with the last-archived value and the last-received value 
    /// for the tag, are used to create an angle; incoming values are rejected if they fall inside 
    /// this angle.  Every incoming value causes the compression angle to narrow, increasing the 
    /// chance of the next incoming value passing through the compression filter.
    /// </para>
    /// 
    /// <para>
    /// The <c>Sinsuoid_Unfiltered</c> tag is primed with exactly the same set of values as the 
    /// <c>Sinusoid</c> tag, but does not use exception or compression filtering, meaning that 
    /// every incoming value will be written to the historian archive.
    /// </para>
    /// 
    /// </remarks>
    public class SampleDataGenerator : HostedService {

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger<SampleDataGenerator> _logger;

        /// <summary>
        /// The Aika historian.
        /// </summary>
        private readonly AikaHistorian _historian;

        /// <summary>
        /// The Aika task runner.
        /// </summary>
        private readonly ITaskRunner _taskRunner;


        /// <summary>
        /// Creates a new <see cref="SampleDataGenerator"/> object.
        /// </summary>
        /// <param name="historian">The Aika historian.</param>
        /// <param name="taskRunner">The Aika task runner.</param>
        /// <param name="logger">The logger for the instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="historian"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="taskRunner"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="logger"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="historian"/> is not configured to use <see cref="Aika.Historians.InMemoryHistorian"/> as its underlying implementation.</exception>
        public SampleDataGenerator(AikaHistorian historian, ITaskRunner taskRunner, ILogger<SampleDataGenerator> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
            _taskRunner = taskRunner ?? throw new ArgumentNullException(nameof(taskRunner));

            if (_historian.Historian.GetType() != typeof(Aika.Historians.InMemoryHistorian)) {
                throw new ArgumentException($"{nameof(SampleDataGenerator)} can only be used with the historian {typeof(Aika.Historians.InMemoryHistorian).FullName} implementation.", nameof(historian));
            }
        }


        /// <summary>
        /// Gets an internal identity that represents the Aika system account.
        /// </summary>
        /// <returns>
        /// A <see cref="System.Security.Claims.ClaimsPrincipal"/> that represents the Aika system 
        /// account.
        /// </returns>
        internal static System.Security.Claims.ClaimsPrincipal GetSystemIdentity() {
            var identity = new System.Security.Claims.ClaimsIdentity("AikaSystem");

            identity.AddClaim(new System.Security.Claims.Claim(identity.RoleClaimType, Aika.AspNetCore.Authorization.Policies.Administrator));
            identity.AddClaim(new System.Security.Claims.Claim(identity.RoleClaimType, Aika.AspNetCore.Authorization.Policies.ManageTags));
            identity.AddClaim(new System.Security.Claims.Claim(identity.RoleClaimType, Aika.AspNetCore.Authorization.Policies.ReadTagData));
            identity.AddClaim(new System.Security.Claims.Claim(identity.RoleClaimType, Aika.AspNetCore.Authorization.Policies.WriteTagData));

            return new System.Security.Claims.ClaimsPrincipal(identity);
        }


        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {


            var identity = GetSystemIdentity();

            var now = DateTime.UtcNow;
            var start = now.AddDays(-1);

            var tag = await _historian.CreateTag(identity,
                                                new TagDefinitionUpdate() {
                                                    Name = "Sinusoid",
                                                    Description = $"12 hour sinusoid wave (starting at {start:dd-MMM-yy HH:mm:ss} UTC), with exception and compression filtering",
                                                    DataType = TagDataType.FloatingPoint,
                                                    ExceptionFilterSettings = new TagValueFilterSettingsUpdate() {
                                                        IsEnabled = true,
                                                        LimitType = TagValueFilterDeviationType.Absolute,
                                                        Limit = 0.1,
                                                        WindowSize = TimeSpan.FromDays(1)
                                                    },
                                                    CompressionFilterSettings = new TagValueFilterSettingsUpdate() {
                                                        IsEnabled = true,
                                                        LimitType = TagValueFilterDeviationType.Absolute,
                                                        Limit = 0.15,
                                                        WindowSize = TimeSpan.FromDays(1)
                                                    }
                                                },
                                                cancellationToken).ConfigureAwait(false);

            var tag_unfiltered = await _historian.CreateTag(identity,
                                                new TagDefinitionUpdate() {
                                                    Name = "Sinusoid_Unfiltered",
                                                    Description = $"12 hour sinusoid wave (starting at {start:dd-MMM-yy HH:mm:ss} UTC), without exception and compression filtering",
                                                    DataType = TagDataType.FloatingPoint,
                                                    ExceptionFilterSettings = new TagValueFilterSettingsUpdate() {
                                                        IsEnabled = false
                                                    },
                                                    CompressionFilterSettings = new TagValueFilterSettingsUpdate() {
                                                        IsEnabled = false
                                                    }
                                                },
                                                cancellationToken).ConfigureAwait(false);

            var samples = new List<TagValue>();
            var wavePeriod = TimeSpan.FromHours(12).Ticks;
            const int waveAmplitude = 50;

            double waveFunc(double time, double period, double amplitude) {
                return amplitude * (Math.Sin(2 * Math.PI * (1 / period) * (time % period)));
            };

            for (var sampleTime = start; sampleTime <= now; sampleTime = sampleTime.Add(TimeSpan.FromMinutes(0.5))) {
                var value = waveFunc(sampleTime.Ticks, wavePeriod, waveAmplitude);
                samples.Add(new TagValue(sampleTime, value, null, TagValueQuality.Good, null));
            }

            await _historian.WriteTagData(identity,
                                         new Dictionary<string, IEnumerable<TagValue>>() {
                                                 { tag.Name, samples },
                                                 { tag_unfiltered.Name, samples }
                                         },
                                         cancellationToken).ConfigureAwait(false);

            do {
                // Every 30 seconds, we'll generate a new sample to write to both tags.
                await Task.Delay(TimeSpan.FromMinutes(0.5), cancellationToken).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }

                var sampleTime = DateTime.UtcNow;
                var value = waveFunc(sampleTime.Ticks, wavePeriod, waveAmplitude);
                var snapshot = new TagValue(sampleTime, value, null, TagValueQuality.Good, null);

                // Use the task runner to perform the actual write, so that we can approximately 
                // keep to our 30 second schedule.
                _taskRunner.RunBackgroundTask(ct => 
                    _historian.WriteTagData(identity,
                                            new Dictionary<string, IEnumerable<TagValue>>() {
                                                { tag.Name, new[] { snapshot } },
                                                { tag_unfiltered.Name, new[] { snapshot } }
                                            },
                                            ct));
            }
            while (!cancellationToken.IsCancellationRequested);
        }
    }
}
