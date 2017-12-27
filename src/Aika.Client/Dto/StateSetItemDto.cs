using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Aika.Client.Dto {
    /// <summary>
    /// Describes a digital state for a tag.
    /// </summary>
    public class StateSetItemDto {

        /// <summary>
        /// Gets or sets the state name.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the state value.
        /// </summary>
        [Required]
        public int Value { get; set; }

    }
}
