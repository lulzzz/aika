using System;
using System.Collections.Generic;
using System.Text;

namespace Aika {

    /// <summary>
    /// Describes the settings for configuring a <see cref="StateSet"/>.
    /// </summary>
    public class StateSetSettings {

        /// <summary>
        /// Gets or sets the state set name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the state set description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the states for the set.
        /// </summary>
        public StateSetItem[] States { get; set; }

    }
}
