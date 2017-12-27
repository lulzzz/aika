using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aika;

namespace Aika.Client.Dto {

    /// <summary>
    /// Describes a historical data query response.
    /// </summary>
    public class HistoricalTagValuesDto {

        /// <summary>
        /// Gets or sets the values.
        /// </summary>
        public IEnumerable<TagValueDto> Values { get; set; }

        /// <summary>
        /// Gets or sets a hint that informs how the values should be visualized on a trend (i.e. 
        /// should the trend interpolate between points in the series, or should it apply a 
        /// trailing-edge transition between two points).
        /// </summary>
        public string VisualizationHint { get; set; }

    }
}
