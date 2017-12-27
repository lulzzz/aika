using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Aika.Client.Dto {

    /// <summary>
    /// Describes an API data write request.
    /// </summary>
    public class WriteTagValuesRequest {

        /// <summary>
        /// Gets or sets the data to write, indexed by tag name.
        /// </summary>
        [Required]
        public IDictionary<string, TagValueDto[]> Data { get; set; }

    }
}
