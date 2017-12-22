using System;
using System.Collections.Generic;
using System.Text;

namespace Aika {

    /// <summary>
    /// Describes an entry in a tag <see cref="StateSet"/>.
    /// </summary>
    public class StateSetItem {

        /// <summary>
        /// Gets the name of the state.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the state value.
        /// </summary>
        public int Value { get; }


        /// <summary>
        /// Creates a new <see cref="StateSetItem"/> object.
        /// </summary>
        /// <param name="name">The name of the state.</param>
        /// <param name="value">The state value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        public StateSetItem(string name, int value) {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
        }

    }

}
