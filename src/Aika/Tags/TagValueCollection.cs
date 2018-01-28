using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Tags {

    /// <summary>
    /// Describes the results of a historical data query for a single tag.
    /// </summary>
    public class TagValueCollection {

        /// <summary>
        /// Gets or sets the values.
        /// </summary>
        public IEnumerable<TagValue> Values { get; set; }

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
        /// * Query results for any historical query that does not use contain <see cref="DataQueryFunction.Interpolated"/> 
        ///   or plot data.
        /// </remarks>
        public TagValueCollectionVisualizationHint VisualizationHint { get; set; }
    }


    /// <summary>
    /// Describes a hint for how a historical data series should be visualized.
    /// </summary>
    public enum TagValueCollectionVisualizationHint {

        /// <summary>
        /// The visualization should use a trailing-edge transition between points.
        /// </summary>
        TrailingEdge,

        /// <summary>
        /// The visualization should interpolate a line between points.
        /// </summary>
        Interpolated
    }
}
