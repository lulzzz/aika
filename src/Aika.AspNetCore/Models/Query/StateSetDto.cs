using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Aika.AspNetCore.Models.Query
{
    public class StateSetDto {

        [Required]
        [MinLength(1)]
        [MaxLength(50)]
        public string Name { get; set; }


        [Required]
        [MinLength(1)]
        public StateSetItemDto[] States { get; set; }


        public StateSetDto() { }


        internal StateSetDto(StateSet stateSet): this() {
            if (stateSet == null) {
                throw new ArgumentNullException(nameof(stateSet));
            }

            Name = stateSet.Name;
            States = stateSet.Select(x => new StateSetItemDto(x)).ToArray();
        }

    }
}
