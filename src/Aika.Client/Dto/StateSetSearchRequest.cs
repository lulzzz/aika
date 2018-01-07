using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Aika.Client.Dto {
    /// <summary>
    /// Describes a state set search request.
    /// </summary>
    public class StateSetSearchRequest {

        /// <summary>
        /// Gets or sets the page size for the results.
        /// </summary>
        [Required]
        [Range(1, 100)]
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the results page to return.
        /// </summary>
        [Required]
        [Range(1, Int32.MaxValue)]
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the state set name filter.
        /// </summary>
        [MaxLength(100)]
        public string Name { get; set; }

    }

}
