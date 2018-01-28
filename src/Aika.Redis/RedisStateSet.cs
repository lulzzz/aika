using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika.StateSets;
using StackExchange.Redis;

namespace Aika.Redis {
    /// <summary>
    /// Defines methods for managing state set definitions.
    /// </summary>
    internal static class RedisStateSet {

        /// <summary>
        /// Loads all state set definitions for the specified historian.
        /// </summary>
        /// <param name="historian">The historian to load the state sets from.</param>
        /// <param name="callback">A callback function that is invoked every time a state set is loaded from Redis.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will load all state sets into the historian.
        /// </returns>
        internal static async Task LoadAll(RedisHistorian historian, Action<StateSet> callback, CancellationToken cancellationToken) {
            var key = historian.GetKeyForStateSetNamesList();

            const int pageSize = 100;
            var page = 0;
            bool @continue;

            // Load state sets 100 at a time.
            do {
                @continue = false;
                ++page;

                long start = (page - 1) * pageSize;
                long end = start + pageSize - 1; // -1 because right-most item is included when getting the list range.

                var names = await historian.Connection.GetDatabase().ListRangeAsync(key, start, end).ConfigureAwait(false);
                @continue = names.Length == pageSize;
                if (names.Length == 0) {
                    continue;
                }

                var tasks = names.Select(x => Task.Run(async () => {
                    var tag = await Load(historian, x, cancellationToken).ConfigureAwait(false);
                    callback(tag);
                })).ToArray();

                await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            } while (@continue);
        }


        /// <summary>
        /// Loads a state set from the Redis database.
        /// </summary>
        /// <param name="historian">The historian to load the state set from.</param>
        /// <param name="name">The name of the state set to load.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the loaded state set.
        /// </returns>
        internal static async Task<StateSet> Load(RedisHistorian historian, string name, CancellationToken cancellationToken) {
            var values = await historian.Connection.GetDatabase().HashGetAllAsync(historian.GetKeyForStateSetDefinition(name)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            string description = null;
            var states = new List<StateSetItem>();

            foreach (var item in values) {
                if (item.Name == "DESC") {
                    description = item.Value;
                    continue;
                }

                var hName = item.Name.ToString();
                if (hName.StartsWith("S_")) {
                    states.Add(new StateSetItem(hName.Substring(2), (int) item.Value));
                }
            }

            return new StateSet(name, description, states);
        }


        /// <summary>
        /// Saves a state set definition to the Redis database.
        /// </summary>
        /// <param name="historian">The historian to load the state set from.</param>
        /// <param name="stateSet">The state set to save.</param>
        /// <param name="addToMasterList">
        ///   Specify <see langword="true"/> when the state set is being created and <see langword="false"/> 
        ///   when it is being updated.
        /// </param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will save the tag definition to the Redis database.
        /// </returns>
        internal static async Task Save(RedisHistorian historian, StateSet stateSet, bool addToMasterList, CancellationToken cancellationToken) {
            var key = historian.GetKeyForStateSetDefinition(stateSet.Name);

            var tasks = new List<Task>();
            var db = historian.Connection.GetDatabase();

            var hashes = new List<HashEntry>() {
                new HashEntry("DESC", stateSet.Description)
            };
            hashes.AddRange(stateSet.Select(x => new HashEntry($"S_{x.Name}", x.Value)));

            tasks.Add(db.HashSetAsync(key, hashes.ToArray()));
            if (addToMasterList) {
                var listKey = historian.GetKeyForStateSetNamesList();
                tasks.Add(db.ListRightPushAsync(listKey, stateSet.Name));
            }

            await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }


        /// <summary>
        /// Deletes a state set definition.
        /// </summary>
        /// <param name="historian">The historian to delete the state set from.</param>
        /// <param name="name">The name of the state set to delete.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will delete the tag.
        /// </returns>
        internal static async Task Delete(RedisHistorian historian, string name, CancellationToken cancellationToken) {
            var tasks = new List<Task>();

            // Delete the state set definition.
            tasks.Add(historian.Connection.GetDatabase().KeyDeleteAsync(historian.GetKeyForStateSetDefinition(name)));

            // Remove the name from the list of state set names.
            var nameListKey = historian.GetKeyForStateSetNamesList();
            tasks.Add(historian.Connection.GetDatabase().ListRemoveAsync(nameListKey, name));

            await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

    }
}
