using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aika.AspNetCore {
    internal static class TaskRunnerExtensions {

        internal static IServiceCollection AddTaskRunnerService(this IServiceCollection services) {
            services.AddSingleton<TaskRunner>();
            services.AddSingleton<IHostedService>(x => x.GetService<TaskRunner>());
            services.AddSingleton<ITaskRunner>(x => x.GetService<TaskRunner>());

            return services;
        }

    }
}
