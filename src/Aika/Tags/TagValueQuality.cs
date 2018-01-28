using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Tags {

    /// <summary>
    /// Describes the quality of a <see cref="TagValue"/>.
    /// </summary>
    public enum TagValueQuality {

        /// <summary>
        /// The value has bad quality.
        /// </summary>
        Bad = 0,

        /// <summary>
        /// The value has uncertain/questionable quality.
        /// </summary>
        Uncertain = 64,

        /// <summary>
        /// The value has good quality.
        /// </summary>
        Good = 192

    }
}
