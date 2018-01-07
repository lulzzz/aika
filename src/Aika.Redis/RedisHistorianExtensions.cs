using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Redis {
    internal static class RedisHistorianExtensions {

        /// <summary>
        /// Gets the Redis key prefix that is used for all things related to a single tag (the definition, the snapshot value, and the raw history).
        /// </summary>
        /// <param name="tagId">The tag ID.</param>
        /// <returns>The key prefix.</returns>
        internal static string GetKeyPrefixForTag(this RedisHistorian historian, string tagId) {
            return $"{historian.RedisKeyPrefix}:tags:{tagId}";
        }


        /// <summary>
        /// Gets the Redis key for the list that contains all of the defined tag IDs.
        /// </summary>
        /// <returns>
        /// The Redis key for the tag IDs list.
        /// </returns>
        /// <remarks>
        /// We use a separate list to store the tag IDs rather than doing a wildcard match on keys (e.g. 
        /// searching for keys matching "aika:tags:*:definition"), 
        /// because this involves searching through all keys on a specific server, rather than on the 
        /// database in general (which may be split across multiple nodes).  See here for an 
        /// explanation: https://stackexchange.github.io/StackExchange.Redis/KeysScan
        /// </remarks>
        internal static string GetKeyForTagIdsList(this RedisHistorian historian) {
            return historian.GetKeyPrefixForTag("tagIds");
        }


        /// <summary>
        /// Gets the Redis key for the hash that defines the tag with the provided tag ID.
        /// </summary>
        /// <param name="tagId">The tag ID.</param>
        /// <returns>
        /// The Redis key for the tag definition hash.
        /// </returns>
        internal static string GetKeyForTagDefinition(this RedisHistorian historian, string tagId) {
            return $"{historian.GetKeyPrefixForTag(tagId)}:definition";
        }


        /// <summary>
        /// Gets the Redis key for things related to raw data.
        /// </summary>
        /// <param name="tagId">The tag ID.</param>
        /// <returns>
        /// The Redis key for the tag's raw data.
        /// </returns>
        internal static string GetKeyForRawData(this RedisHistorian historian, string tagId) {
            return $"{historian.GetKeyPrefixForTag(tagId)}:rawValues";
        }


        /// <summary>
        /// Gets the Redis key for the tag's snapshot value.
        /// </summary>
        /// <param name="tagId">The tag ID.</param>
        /// <returns>
        /// The Redis key for the tag's snapshot value.
        /// </returns>
        internal static string GetKeyForSnapshotData(this RedisHistorian historian, string tagId) {
            return $"{historian.GetKeyPrefixForTag(tagId)}:snapshotValue";
        }

    }
}
