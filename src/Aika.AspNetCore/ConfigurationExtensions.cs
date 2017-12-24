using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aika.AspNetCore {
    public static class ConfigurationExtensions {

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


        public static IMvcBuilder AddAikaRoutes(this IMvcBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddApplicationPart(typeof(ConfigurationExtensions).Assembly);
            return builder;
        }

    }
}
