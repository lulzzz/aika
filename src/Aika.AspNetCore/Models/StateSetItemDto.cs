using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Aika.AspNetCore.Models {
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


        /// <summary>
        /// Creates a new <see cref="StateSetItemDto"/> object.
        /// </summary>
        public StateSetItemDto() { }


        /// <summary>
        /// Creates a new <see cref="StateSetItemDto"/> object from the specified <see cref="StateSetItem"/>.
        /// </summary>
        /// <param name="item">The <see cref="StateSetItem"/> to copy the configuration from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
        internal StateSetItemDto(Aika.StateSetItem item) : this() {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }

            Name = item.Name;
            Value = item.Value;
        }


        /// <summary>
        /// Converts the object to an equivalent <see cref="StateSetItem"/>.
        /// </summary>
        /// <returns>The equivalent <see cref="StateSetItem"/>.</returns>
        internal Aika.StateSetItem ToStateSetItem() {
            return new StateSetItem(Name, Value);
        }

    }
}
