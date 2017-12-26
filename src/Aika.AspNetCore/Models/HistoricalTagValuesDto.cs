using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aika;

namespace Aika.AspNetCore.Models {

    /// <summary>
    /// Web API model for a historical data query response.
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
        /// <remarks>
        /// Trailing-edge visualization is recommended in the following scenarios:
        /// 
        /// * All data for digital/state-based tags, regardless of the type of aggregation used.
        /// * All raw, unaggregated data.
        /// * Query results for any historical query that does not use the <see cref="DataQueryFunction.Interpolated"/> 
        ///   or <see cref="DataQueryFunction.Plot"/>.
        /// </remarks>
        public TagValueCollectionVisualizationHint VisualizationHint { get; set; }


        /// <summary>
        /// Creates a new <see cref="HistoricalTagValuesDto"/> object.
        /// </summary>
        public HistoricalTagValuesDto() { }


        /// <summary>
        /// Creates a new <see cref="HistoricalTagValuesDto"/> object using the specified <see cref="TagValueCollection"/> object.
        /// </summary>
        /// <param name="values">The <see cref="TagValueCollection"/> object to copy values from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
        internal HistoricalTagValuesDto(TagValueCollection values) : this() {
            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }

            Values = values.Values.Select(x => new TagValueDto(x)).ToArray();
            VisualizationHint = values.VisualizationHint;
        }

    }
}
