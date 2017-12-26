using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Aika.AspNetCore.Models {

    /// <summary>
    /// Describes a set of discrete states that can be associated with tags.
    /// </summary>
    public class StateSetDto {

        /// <summary>
        /// Gets or sets the name of the set.
        /// </summary>
        [Required]
        [MinLength(1)]
        [MaxLength(50)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the states.
        /// </summary>
        [Required]
        [MinLength(1)]
        public StateSetItemDto[] States { get; set; }


        /// <summary>
        /// Creates a new <see cref="StateSetDto"/> object.
        /// </summary>
        public StateSetDto() { }


        /// <summary>
        /// Creates a new <see cref="StateSetDto"/> from an existing <see cref="StateSet"/> object.
        /// </summary>
        /// <param name="stateSet">The state set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stateSet"/> is <see langword="null"/>.</exception>
        internal StateSetDto(StateSet stateSet) : this() {
            if (stateSet == null) {
                throw new ArgumentNullException(nameof(stateSet));
            }

            Name = stateSet.Name;
            States = stateSet.Select(x => new StateSetItemDto(x)).ToArray();
        }

    }
}
