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
using Microsoft.Extensions.Hosting;
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
                // Allow Aika SignalR requests to be authenticated by putting the access token in the 
                // query string.
                options.Events = new JwtBearerEvents() {
                    OnMessageReceived = context => {
                        var token = context.Request.GetTokenFromAikaHubQueryString();
                        if (token != null) {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // Configure authorization based on scopes in the JWTs.
            services.AddScopeBasedAikaAuthorizationPolicies(Configuration.GetValue<string>(JwtAuthoritySetting));

            // Register the Aika historian and the underlying implementation.
            services.AddAikaHistorian<Aika.Historians.InMemoryHistorian>();

            // Add MVC and register the Aika-specific routes.
            services.AddMvc().AddAikaRoutes();

            // Add SignalR.
            services.AddSignalR();

            // Add our sample data generator.
            services.AddSingleton<IHostedService, SampleDataGenerator>();
        }


        /// <summary>
        /// Configures the request pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="env">The hosting envrionment.</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();
            app.UseMvc();
            app.UseSignalR(x => x.MapAikaHubs()); // Make sure that the Aika hubs are mapped.
        }

    }
}
