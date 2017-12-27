using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Aika.Client.Dto {

    /// <summary>
    /// Describes a snapshot data request.
    /// </summary>
    public class SnapshotDataRequest {

        /// <summary>
        /// Gets or sets the tags to query.
        /// </summary>
        [Required]
        [MinLength(1)]
        [MaxLength(100)]
        public string[] Tags { get; set; }

    }
}
