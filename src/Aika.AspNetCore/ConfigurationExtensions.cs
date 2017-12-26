using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aika.AspNetCore {

    /// <summary>
    /// Extension methods for adding Aika configuration to an ASP.NET core application.
    /// </summary>
    public static class ConfigurationExtensions {

        /// <summary>
        /// Adds an Aika historian service to the specified services container.
        /// </summary>
        /// <typeparam name="T">The <see cref="IHistorian"/> implementation to use with the <see cref="AikaHistorian"/>.</typeparam>
        /// <param name="services">The services container to add the historian service to.</param>
        /// <returns>
        /// The <paramref name="services"/> container, to allow chaining.
        /// </returns>
        public static IServiceCollection AddAikaHistorian<T>(this IServiceCollection services) where T : class, IHistorian {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<TaskRunner>();
            services.AddSingleton<IHostedService>(x => x.GetService<TaskRunner>());
            services.AddSingleton<ITaskRunner>(x => x.GetService<TaskRunner>());

            services.AddSingleton<IHistorian, T>();
            services.AddSingleton<AikaHistorian>();

            return services;
        }


        /// <summary>
        /// Adds Aika authorization policies that use scope claims to determine if a caller is authorized.  
        /// Use this method when configuring authorization for a system that is using JWTs to authenticate
        /// callers.
        /// </summary>
        /// <param name="services">The service collection to add the authorization policies to.</param>
        /// <param name="issuer">
        ///   The issuer that must be matched for a scope claim to be valid (i.e. the authority that 
        ///   issued the JWT).  The <see cref="Authorization.Scopes"/> class contains constants for the 
        ///   possible scopes.
        /// </param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>, to allow chaining.
        /// </returns>
        /// <seealso cref="Authorization.Scopes"/>
        public static IServiceCollection AddAikaAuthorizationScopePolicies(this IServiceCollection services, string issuer) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddAuthorization(options => {
                options.AddPolicy(Authorization.Scopes.Administrator, policy => policy.Requirements.Add(new Authorization.HasScopeRequirement(Authorization.Scopes.Administrator, issuer)));
                options.AddPolicy(Authorization.Scopes.ManageTags, policy => policy.Requirements.Add(new Authorization.HasScopeRequirement(Authorization.Scopes.ManageTags, issuer)));
                options.AddPolicy(Authorization.Scopes.ReadTagData, policy => policy.Requirements.Add(new Authorization.HasScopeRequirement(Authorization.Scopes.ReadTagData, issuer)));
                options.AddPolicy(Authorization.Scopes.WriteTagData, policy => policy.Requirements.Add(new Authorization.HasScopeRequirement(Authorization.Scopes.WriteTagData, issuer)));
            });

            services.AddSingleton<IAuthorizationHandler, Authorization.HasScopeHandler>();

            return services;
        }


        /// <summary>
        /// Adds Aika authorization policies that use role membership to determine if a caller is authorized.  
        /// Use this method when configuring authorization for a system that is using e.g. Windows authentication.
        /// </summary>
        /// <param name="services">The service collection to add the authorization policies to.</param>
        /// <param name="getRolesForScope">
        ///   A callback function that receives the Aika scope name being configured, and returns the 
        ///   roles that are authorized to perform that scope.  The <see cref="Authorization.Scopes"/>
        ///   class contains constants for the possible scopes.
        /// </param>
        /// <returns>
        /// 
        /// </returns>
        /// <seealso cref="Authorization.Scopes"/>
        public static IServiceCollection AddAikaAuthorizationRolePolicies(this IServiceCollection services, Func<string, IEnumerable<string>> getRolesForScope) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }
            if (getRolesForScope == null) {
                throw new ArgumentNullException(nameof(getRolesForScope));
            }

            services.AddAuthorization(options => {
                options.AddPolicy(Authorization.Scopes.Administrator, policy => policy.RequireRole(getRolesForScope.Invoke(Authorization.Scopes.Administrator)));
                options.AddPolicy(Authorization.Scopes.ManageTags, policy => policy.RequireRole(getRolesForScope.Invoke(Authorization.Scopes.ManageTags)));
                options.AddPolicy(Authorization.Scopes.ReadTagData, policy => policy.RequireRole(getRolesForScope.Invoke(Authorization.Scopes.ReadTagData)));
                options.AddPolicy(Authorization.Scopes.WriteTagData, policy => policy.RequireRole(getRolesForScope.Invoke(Authorization.Scopes.Administrator)));
            });

            return services;
        }


        /// <summary>
        /// Adds Aika API routes to an <see cref="IMvcBuilder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/> to add the routes to.</param>
        /// <returns>
        /// The <paramref name="builder"/>, to allow method chaining.
        /// </returns>
        public static IMvcBuilder AddAikaRoutes(this IMvcBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddApplicationPart(typeof(ConfigurationExtensions).Assembly);
            return builder;
        }


        /// <summary>
        /// Maps Aika SignalR routes.
        /// </summary>
        /// <param name="hubRouteBuilder">The <see cref="HubRouteBuilder"/> to register the SignalR routes with.</param>
        /// <returns>
        /// The same <paramref name="hubRouteBuilder"/>, to allow method chaining.
        /// </returns>
        public static HubRouteBuilder MapAikaHubs(this HubRouteBuilder hubRouteBuilder) {
            if (hubRouteBuilder == null) {
                throw new ArgumentNullException(nameof(hubRouteBuilder));
            }

            hubRouteBuilder.MapHub<Hubs.SnapshotHub>("aika/hubs/snapshot");

            return hubRouteBuilder;
        }

    }
}
