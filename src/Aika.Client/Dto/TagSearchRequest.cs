using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Aika;

namespace Aika.Client.Dto {

    /// <summary>
    /// Describes a tag search.
    /// </summary>
    public class TagSearchRequest {

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
        /// Gets or sets the join type for the search filters (i.e. should filters be AND'd or OR'd together).
        /// </summary>
        [Required]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the tag name filter.
        /// </summary>
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the tag description filter.
        /// </summary>
        [MaxLength(100)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the tag units filter.
        /// </summary>
        [MaxLength(100)]
        public string Units { get; set; }

    }
}
