using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aika.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aika.SampleApp {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddLogging(x => x.AddConsole().AddDebug());
            services.AddAuthentication();
            services.AddAikaHistorian<Aika.Historians.InMemoryHistorian>();
            services.AddMvc().AddAikaRoutes().AddJsonOptions(x => {
                x.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                x.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            });
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            AddTestTagData(app.ApplicationServices);

            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            // Middleware that adds all callers to all Aika roles.
            app.Use((context, next) => {
                context.User = new System.Security.Claims.ClaimsPrincipal(GetSystemIdentity());
                return next();
            });

            app.UseMvc();
        }


        private System.Security.Claims.ClaimsIdentity GetSystemIdentity() {
            var identity = new System.Security.Claims.ClaimsIdentity("Aika");
            identity.AddClaim(new System.Security.Claims.Claim(identity.NameClaimType, "AikaSystem"));
            identity.AddClaim(new System.Security.Claims.Claim(identity.RoleClaimType, Roles.Administrator));
            identity.AddClaim(new System.Security.Claims.Claim(identity.RoleClaimType, Roles.ManageTags));
            identity.AddClaim(new System.Security.Claims.Claim(identity.RoleClaimType, Roles.ReadTagData));
            identity.AddClaim(new System.Security.Claims.Claim(identity.RoleClaimType, Roles.WriteTagData));

            return identity;
        }


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
                    samples.Add(new TagValue(sampleTime, value, value.ToString(), TagValueQuality.Good, null));
                }

                await historian.WriteTagData(identity, new Dictionary<string, IEnumerable<TagValue>>() { { tag.Name, samples } }, ct).ConfigureAwait(false);

                do {
                    await Task.Delay(TimeSpan.FromMinutes(1), ct).ConfigureAwait(false);
                    if (ct.IsCancellationRequested) {
                        break;
                    }

                    var sampleTime = DateTime.UtcNow;
                    var value = waveFunc(sampleTime.Ticks, wavePeriod, 50);
                    var snapshot = new TagValue(sampleTime, value, value.ToString(), TagValueQuality.Good, null);

                    taskRunner.RunBackgroundTask(ct2 => historian.WriteTagData(identity, new Dictionary<string, IEnumerable<TagValue>>() { { tag.Name, new[] { snapshot } } }, ct2));
                }
                while (!ct.IsCancellationRequested);

            });
        }
    }
}
