using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aika.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aika.SampleApp {

    /// <summary>
    /// ASP.NET Core startup class.
    /// </summary>
    public class Startup {

        /// <summary>
        /// Environment variable that defines the JWT authority.
        /// </summary>
        private const string JwtAuthoritySetting = "AIKA_JWTAUTHORITY";

        /// <summary>
        /// Environment variable that defines the JWT audience.
        /// </summary>
        private const string JwtAudienceSetting = "AIKA_JWTAUDIENCE";

        /// <summary>
        /// Gets the configuration object.
        /// </summary>
        public IConfiguration Configuration { get; }


        /// <summary>
        /// Creates a new <see cref="Startup"/> object.
        /// </summary>
        /// <param name="configuration">The configuration object.</param>
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }


        /// <summary>
        /// Configures application services.
        /// </summary>
        /// <param name="services">The container to add services to.</param>
        public void ConfigureServices(IServiceCollection services) {
            services.AddLogging(x => x.AddConsole().AddDebug());

            // Configure JWT authentication.
            services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options => {
                options.Authority = Configuration.GetValue<string>(JwtAuthoritySetting);
                options.Audience = Configuration.GetValue<string>(JwtAudienceSetting);
                // Allow SignalR websocket upgrades to be authenticated by putting the JWT in the 
                // query string.
                options.Events = new JwtBearerEvents() {
                    OnMessageReceived = context => {
                        if (context.Request.Path.Value.Contains("/aika/hubs/") && context.Request.Query.TryGetValue("token", out var value)) {
                            context.Token = value;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // Configure authorization based on scopes in the JWTs.
            services.AddAikaAuthorizationScopePolicies(Configuration.GetValue<string>(JwtAuthoritySetting));

            // Register the Aika historian and the underlying implementation.
            services.AddAikaHistorian<Aika.Historians.InMemoryHistorian>();

            // Add MVC and register the Aika-specific routes.
            services.AddMvc().AddAikaRoutes().AddJsonOptions(x => {
                x.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                x.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            });

            // Add SignalR.
            services.AddSignalR(x => x.JsonSerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter()));
        }


        /// <summary>
        /// Configures the request pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="env">The hosting envrionment.</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            AddTestTagData(app.ApplicationServices);

            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();

            app.UseMvc();
            app.UseSignalR(x => x.MapAikaHubs()); // Make sure that the Aika hubs are mapped.
        }


        /// <summary>
        /// Gets an internal identity that represents the Aika system account.
        /// </summary>
        /// <returns>
        /// A <see cref="System.Security.Claims.ClaimsPrincipal"/> that represents the Aika system 
        /// account.
        /// </returns>
        private System.Security.Claims.ClaimsPrincipal GetSystemIdentity() {
            var identity = new System.Security.Claims.ClaimsIdentity("AikaSystem");
            return new System.Security.Claims.ClaimsPrincipal(identity);
        }


        /// <summary>
        /// Creates test tag data and adds it to the Aika historian.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        private void AddTestTagData(IServiceProvider serviceProvider) {
            var taskRunner = serviceProvider.GetService<ITaskRunner>();
            var historian = serviceProvider.GetService<AikaHistorian>();
            var log = serviceProvider.GetService<ILoggerFactory>().CreateLogger<Startup>();

            taskRunner.RunBackgroundTask(async ct => {
                var identity = GetSystemIdentity();

                var now = DateTime.UtcNow;
                var start = now.AddDays(-1);

                var tag = await historian.CreateTag(identity,
                                                    new TagDefinitionUpdate() {
                                                        Name = "Sinusoid",
                                                        Description = $"12 hour sinusoid wave (starting at {start:dd-MMM-yy HH:mm:ss} UTC)",
                                                        DataType = TagDataType.FloatingPoint,
                                                        ExceptionFilterSettings = new TagValueFilterSettingsUpdate() {
                                                            IsEnabled = true,
                                                            LimitType = TagValueFilterDeviationType.Absolute,
                                                            Limit = 0.5,
                                                            WindowSize = TimeSpan.FromDays(1)
                                                        },
                                                        CompressionFilterSettings = new TagValueFilterSettingsUpdate() {
                                                            IsEnabled = true,
                                                            LimitType = TagValueFilterDeviationType.Absolute,
                                                            Limit = 0.75,
                                                            WindowSize = TimeSpan.FromDays(1)
                                                        }
                                                    },
                                                    ct).ConfigureAwait(false);

                log.LogDebug($"Created tag \"{tag.Name}\" ({tag.Id}).");

                var samples = new List<TagValue>();
                var wavePeriod = TimeSpan.FromHours(12).Ticks;

                Func<double, double, double, double> waveFunc = (time, period, amplitude) => {
                    return amplitude * (Math.Sin(2 * Math.PI * (1 / period) * (time % period)));
                };

                for (var sampleTime = start; sampleTime <= now; sampleTime = sampleTime.Add(TimeSpan.FromMinutes(1))) {
                    var value = waveFunc(sampleTime.Ticks, wavePeriod, 50);
                    samples.Add(new TagValue(sampleTime, value, null, TagValueQuality.Good, null));
                }

                await historian.InsertTagData(identity, new Dictionary<string, IEnumerable<TagValue>>() { { tag.Name, samples } }, ct).ConfigureAwait(false);

                do {
                    await Task.Delay(TimeSpan.FromSeconds(1), ct).ConfigureAwait(false);
                    if (ct.IsCancellationRequested) {
                        break;
                    }

                    var sampleTime = DateTime.UtcNow;
                    var value = waveFunc(sampleTime.Ticks, wavePeriod, 50);
                    var snapshot = new TagValue(sampleTime, value, null, TagValueQuality.Good, null);

                    taskRunner.RunBackgroundTask(ct2 => historian.WriteTagData(identity, new Dictionary<string, IEnumerable<TagValue>>() { { tag.Name, new[] { snapshot } } }, ct2));
                }
                while (!ct.IsCancellationRequested);

            });
        }
    }
}
