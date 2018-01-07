using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using StackExchange.Redis;

namespace Aika.Redis {

    /// <summary>
    /// Describes configuration options for a <see cref="RedisHistorian"/>.
    /// </summary>
    public sealed class RedisHistorianOptions {

        /// <summary>
        /// The default Redis key prefix to use, if <see cref="KeyPrefix"/> is not specified.
        /// </summary>
        public const string DefaultKeyPrefix = "aika";

        /// <summary>
        /// The prefix to use for all Redis keys for the historian.  If not specified, 
        /// <see cref="DefaultKeyPrefix"/> will be used.
        /// </summary>
        public string KeyPrefix { get; set; }

        /// <summary>
        /// Gets or sets the Redis configuration options.  See 
        /// https://stackexchange.github.io/StackExchange.Redis/Configuration for details.
        /// </summary>
        public string RedisConfigurationOptions { get; set; }

    }
}
