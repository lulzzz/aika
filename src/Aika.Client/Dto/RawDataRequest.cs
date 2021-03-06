﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Aika.Client.Dto {

    /// <summary>
    /// Describes a data request for raw, unprocessed tag data.
    /// </summary>
    public class RawDataRequest : HistoricalDataRequest {

        /// <summary>
        /// Gets or sets the maximum number of samples to retrieve per tag.
        /// </summary>
        [Required]
        [Range(0, Int32.MaxValue)]
        public int PointCount { get; set; }

    }
}
