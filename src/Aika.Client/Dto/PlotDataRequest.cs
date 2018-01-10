using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Aika.Client.Dto
{
    /// <summary>
    /// Describes a request for plot (i.e. visualization-friendly) tag data.
    /// </summary>
    public class PlotDataRequest : HistoricalDataRequest {

        /// <summary>
        /// Gets or sets the number of intervals in the plot request.  This is typically the number of 
        /// horizontal pixels in the trend that will be rendered with the data.  Note that the exact 
        /// number of samples returned may vary depending on the underlying raw points covered by the 
        /// query time range.
        /// </summary>
        [Required]
        [Range(1, Int32.MaxValue)]
        public int Intervals { get; set; }

    }
}
