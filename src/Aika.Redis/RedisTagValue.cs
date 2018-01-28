using System;
using System.Collections.Generic;
using System.Text;
using Aika.Tags;
using Newtonsoft.Json;

namespace Aika.Redis {
    /// <summary>
    /// Represents a data sample stored in Redis.  This class is used to reduce the size of the JSON 
    /// data stored in Redis for a tag value.
    /// </summary>
    public class RedisTagValue {

        /// <summary>
        /// Gets or sets the UTC sample time.
        /// </summary>
        [JsonProperty("TS")]
        public DateTime UtcSampleTime { get; set; }

        /// <summary>
        /// Gets or sets the numeric value.
        /// </summary>
        [JsonProperty("N")]
        public double NumericValue { get; set; }

        /// <summary>
        /// Gets or sets the text value.
        /// </summary>
        [JsonProperty("T")]
        public string TextValue { get; set; }

        /// <summary>
        /// Gets or sets the quality.
        /// </summary>
        [JsonProperty("Q")]
        public int Quality { get; set; }

        /// <summary>
        /// Gets or sets the units.
        /// </summary>
        [JsonProperty("U")]
        public string Units { get; set; }


        /// <summary>
        /// Converts the <see cref="RedisTagValue"/> to a standard Aika <see cref="TagValue"/>.
        /// </summary>
        /// <returns></returns>
        internal TagValue ToTagValue() {
            return new TagValue(UtcSampleTime, NumericValue, TextValue, (TagValueQuality) Quality, Units);
        }


        /// <summary>
        /// Creates a new <see cref="RedisTagValue"/> from a standard Aika <see cref="TagValue"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static RedisTagValue FromTagValue(TagValue value) {
            return new RedisTagValue() {
                UtcSampleTime = value.UtcSampleTime,
                NumericValue = value.NumericValue,
                TextValue = value.TextValue,
                Quality = (int) value.Quality,
                Units = value.Units
            };
        }

    }
}
