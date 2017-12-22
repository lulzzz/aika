using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Aika {

    /// <summary>
    /// Describes a set of discrete states for a state-based tag.
    /// </summary>
    public class StateSet : IEnumerable<StateSetItem> {

        /// <summary>
        /// The states, indexed by name.
        /// </summary>
        private readonly ConcurrentDictionary<string, StateSetItem> _states = new ConcurrentDictionary<string, StateSetItem>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the name of the state set.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the state with the specified name.
        /// </summary>
        /// <param name="name">The state name.</param>
        /// <returns>
        /// The matching <see cref="StateSetItem"/>, or <see langword="null"/> if no matching state is found.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        public StateSetItem this[string name] {
            get {
                if (name == null) {
                    throw new ArgumentNullException(nameof(name));
                }
                return _states.TryGetValue(name, out var result) ? result : null;
            }
        }


        /// <summary>
        /// Gets the names of the registered states.
        /// </summary>
        public IEnumerable<string> StateNames {
            get { return _states.Keys; }
        }


        /// <summary>
        /// Creates a new <see cref="StateSet"/> object.
        /// </summary>
        /// <param name="name">The state set name.</param>
        /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or white space.</exception>
        public StateSet(string name) {
            if (String.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("You must specify a state set name.", nameof(name));
            }
            Name = name.Trim();
        }


        /// <summary>
        /// Creates a new <see cref="StateSet"/> object using the specified state definitions.
        /// </summary>
        /// <param name="name">The state set name.</param>
        /// <param name="states">The state definitions to add.</param>
        /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or white space.</exception>
        public StateSet(string name, IEnumerable<StateSetItem> states) : this(name) {
            if (states != null) {
                foreach (var item in states) {
                    Add(item);
                }
            }
        }


        /// <summary>
        /// Adds a state to the <see cref="StateSet"/>.
        /// </summary>
        /// <param name="state">The state definition to add.</param>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> is <see langword="null"/>.</exception>
        private void Add(StateSetItem state) {
            if (state == null) {
                throw new ArgumentNullException(nameof(state));
            }

            _states[state.Name] = state;
        }


        /// <summary>
        /// Gets an enumerator for the registered states.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate over the registered states.</returns>
        public IEnumerator<StateSetItem> GetEnumerator() {
            return _states.Values.GetEnumerator();
        }


        /// <summary>
        /// Gets an enumerator for the registered states.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate over the registered states.</returns>
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
