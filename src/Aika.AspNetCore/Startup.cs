using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aika.AspNetCore {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddLogging(x => x.AddConsole());
            services.AddAuthentication();
            services.AddMvc();
            services.AddTaskRunnerService();
            services.AddSingleton<IHistorian, Aika.Historians.InMemoryHistorian>();
            services.AddSingleton<AikaHistorian>();
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            // Middleware that adds all callers to all Aika roles.
            app.Use((context, next) => {
                var identity = new System.Security.Claims.ClaimsIdentity("fixed");
                identity.AddClaim(new System.Security.Claims.Claim(identity.NameClaimType, "TestUser"));
                identity.AddClaim(new System.Security.Claims.Claim(identity.RoleClaimType, Roles.Administrator));
                identity.AddClaim(new System.Security.Claims.Claim(identity.RoleClaimType, Roles.ManageTags));
                identity.AddClaim(new System.Security.Claims.Claim(identity.RoleClaimType, Roles.ReadTagData));
                identity.AddClaim(new System.Security.Claims.Claim(identity.RoleClaimType, Roles.WriteTagData));

                context.User = new System.Security.Claims.ClaimsPrincipal(identity);

                return next();
            });

            app.UseMvc();
        }
    }
}
