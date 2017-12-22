using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Aika;
using Aika.AspNetCore.Models.Query;

namespace Aika.AspNetCore.Models.Write {

    /// <summary>
    /// Describes an API data write request.
    /// </summary>
    public class WriteTagValuesRequest {

        /// <summary>
        /// Gets or sets the data to write, indexed by tag name.
        /// </summary>
        [Required]
        IDictionary<string, TagValueDto[]> Data { get; set; }


        /// <summary>
        /// Converts the object into the format required for an <see cref="ITagDataWriter"/> write call.
        /// </summary>
        /// <returns>
        /// A dictionary that maps from case-insensitive tag name to tag value.
        /// </returns>
        internal IDictionary<string, IEnumerable<TagValue>> ToTagValueDictionary() {
            if (Data == null) {
                return new Dictionary<string, IEnumerable<TagValue>>();
            }

            return Data.ToLookup(x => x.Key, 
                                 x => x.Value, 
                                 StringComparer.OrdinalIgnoreCase)
                       .ToDictionary(x => x.Key, 
                                     x => x.SelectMany(y => y.Select(dto => dto.ToTagValue()))
                                           .ToArray()
                                           .AsEnumerable());
        }

    }
}
