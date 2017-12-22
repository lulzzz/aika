using System;
using System.Collections.Generic;
using System.Text;

namespace Aika {
    /// <summary>
    /// Describes the data type for a tag.
    /// </summary>
    public enum TagDataType {
        /// <summary>
        /// The tag contains floating-point data.
        /// </summary>
        FloatingPoint,
        /// <summary>
        /// The tag contains integer data.
        /// </summary>
        Integer,
        /// <summary>
        /// The tag contains text data.
        /// </summary>
        Text,
        /// <summary>
        /// The tag defines a set of possible named states (e.g. ON/OFF).
        /// </summary>
        State
    }
}
