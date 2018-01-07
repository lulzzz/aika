using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
            return services.AddAikaHistorian<T>(null, null);
        }


        /// <summary>
        /// Adds an Aika historian service to the specified services container.
        /// </summary>
        /// <typeparam name="T">The <see cref="IHistorian"/> implementation to use with the <see cref="AikaHistorian"/>.</typeparam>
        /// <param name="services">The services container to add the historian service to.</param>
        /// <param name="options">The Aika service options.</param>
        /// <returns>
        /// The <paramref name="services"/> container, to allow chaining.
        /// </returns>
        public static IServiceCollection AddAikaHistorian<T>(this IServiceCollection services, AikaServiceOptions options) where T : class, IHistorian {
            return services.AddAikaHistorian<T>(null, options);
        }


        /// <summary>
        /// Adds an Aika historian service to the specified services container.
        /// </summary>
        /// <typeparam name="T">The <see cref="IHistorian"/> implementation to use with the <see cref="AikaHistorian"/>.</typeparam>
        /// <param name="services">The services container to add the historian service to.</param>
        /// <param name="historianFactory">The factory to create the singleton instance of <typeparamref name="T"/> with.</param>
        /// <param name="options">The Aika service options.</param>
        /// <returns>
        /// The <paramref name="services"/> container, to allow chaining.
        /// </returns>
        public static IServiceCollection AddAikaHistorian<T>(this IServiceCollection services, Func<IServiceProvider, T> historianFactory, AikaServiceOptions options) where T : class, IHistorian {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<TaskRunner>();
            services.AddSingleton<IHostedService>(x => x.GetService<TaskRunner>());
            services.AddSingleton<ITaskRunner>(x => x.GetService<TaskRunner>());

            if (historianFactory == null) {
                services.AddSingleton<IHistorian, T>();
            }
            else {
                services.AddSingleton<IHistorian, T>(historianFactory);
            }
            services.AddSingleton<AikaHistorian>();

            services.AddSingleton<IHostedService, AikaService>(x => {
                var aika = x.GetService<AikaHistorian>();
                return new AikaService(aika, options ?? new AikaServiceOptions(), x.GetService<ILogger<AikaService>>());
            });

            return services;
        }


        /// <summary>
        /// Adds Aika authorization policies.
        /// </summary>
        /// <param name="services">The service collection to add the authorization policies to.</param>
        /// <param name="configurePolicy">
        ///   A callback function that is called for each Aika policy name, and is passed the policy 
        ///   name and an <see cref="AuthorizationPolicyBuilder"/> that is used to configure the 
        ///   policy.
        /// </param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>, to allow chaining.
        /// </returns>
        public static IServiceCollection AddAikaAuthorizationPolicies(this IServiceCollection services, Action<string, AuthorizationPolicyBuilder> configurePolicy) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }
            if (configurePolicy == null) {
                throw new ArgumentNullException(nameof(configurePolicy));
            }

            services.AddAuthorization(options => {
                var policyNames = new[] {
                    Authorization.Policies.Administrator,
                    Authorization.Policies.ManageTags,
                    Authorization.Policies.ReadTagData,
                    Authorization.Policies.WriteTagData
                };

                foreach (var policyName in policyNames) {
                    options.AddPolicy(policyName, policy => configurePolicy(policyName, policy));
                }
            });

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
        ///   issued the JWT).
        /// </param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>, to allow chaining.
        /// </returns>
        /// <seealso cref="Authorization.Policies"/>
        public static IServiceCollection AddScopeBasedAikaAuthorizationPolicies(this IServiceCollection services, string issuer) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddAikaAuthorizationPolicies((policyName, policy) => policy.Requirements.Add(new Authorization.HasScopeRequirement(policyName, issuer)));
            services.AddSingleton<IAuthorizationHandler, Authorization.HasScopeHandler>();

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
            hubRouteBuilder.MapHub<Hubs.DataFilterHub>("aika/hubs/datafilter");

            return hubRouteBuilder;
        }


        /// <summary>
        /// Gets an access token passed to an Aika SignalR hub via a query string parameter named <c>token</c>.
        /// </summary>
        /// <param name="request">The incoming HTTP request.</param>
        /// <returns>
        /// The token, or <see langword="null"/> if the HTTP request does not specify a <c>token</c> 
        /// parameter in the query string.
        /// </returns>
        public static string GetTokenFromAikaHubQueryString(this HttpRequest request) {
            if (request != null && request.Path.Value.Contains("/aika/hubs/") && request.Query.TryGetValue("token", out var value)) {
                return value;
            }

            return null;
        }

    }
}
