using System;
using System.Collections.Generic;
using System.Text;

namespace Aika.Client.Dto {

    /// <summary>
    /// Describes a data aggregation function supported by the historian.
    /// </summary>
    public class HistorianDataFunctionDto {

        /// <summary>
        /// Gets or sets the function name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the function description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a flag that indicates if the function is natively-supported by the historian, 
        /// or if it uses the built-in Aika implementation.
        /// </summary>
        /// <remarks>
        /// Non-native functions will typically by slower to execute than native functions, since 
        /// non-native functions require Aika to request raw data samples from the underlying historian 
        /// prior to performing the aggregation.  Native functions are processed directly by the 
        /// underlying historian.
        /// </remarks>
        public bool IsNativeFunction { get; set; }

    }
}
