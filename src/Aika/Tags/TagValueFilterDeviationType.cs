using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Tags {

    /// <summary>
    /// Describes the behaviour of an exception or compression deviation threshold value.
    /// </summary>
    public enum TagValueFilterDeviationType {
        /// <summary>
        /// The threshold is expressed as an absolute value change from the filter's previous exceptional value.
        /// </summary>
        Absolute,
        /// <summary>
        /// The threshold is expressed as a fraction of the filter's previous exceptional value.
        /// </summary>
        Fraction,
        /// <summary>
        /// The threshold is expressed as a percentage of the filter's previous exceptional value.
        /// </summary>
        Percentage
    }

}
