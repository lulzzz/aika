using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Aika.Client.Dto {

    /// <summary>
    /// Describes a set of discrete states that can be associated with tags.
    /// </summary>
    public class StateSetDto {

        /// <summary>
        /// Gets or sets the name of the set.
        /// </summary>
        /// <remarks>
        /// No <see cref="RequiredAttribute"/> or <see cref="MinLengthAttribute"/> is specified, because 
        /// it is acceptable to not specify a name when updating an existing state set.
        /// </remarks>
        [MaxLength(50)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description for the set.
        /// </summary>
        [MaxLength(100)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the states.
        /// </summary>
        [Required]
        [MinLength(1)]
        public StateSetItemDto[] States { get; set; }

    }
}
