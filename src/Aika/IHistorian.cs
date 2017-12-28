using System;
using System.Collections.Generic;
using System.Text;

namespace Aika {

    /// <summary>
    /// Interface that marks a class as a back-end data historian.
    /// </summary>
    /// <remarks>
    /// To implement specific areas of historian functionality, <see cref="IHistorian"/> implementations 
    /// should also implement one or more of the <see cref="ITagDataReader"/>, <see cref="ITagDataWriter"/>, 
    /// and <see cref="ITagManager"/> interfaces, depending on the capabilities of the underlying 
    /// historian.
    /// </remarks>
    /// <seealso cref="ITagSearch"/>
    /// <seealso cref="ITagDataReader"/>
    /// <seealso cref="ITagDataWriter"/>
    /// <seealso cref="ITagManager"/>
    public interface IHistorian : ITagSearch {

        /// <summary>
        /// When implemented in a derived type, gets a description for the <see cref="IHistorian"/> 
        /// implementation.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// When implemented in a derived type, gets bespoke properties associated with the 
        /// <see cref="IHistorian"/> implementation.
        /// </summary>
        IDictionary<string, object> Properties { get; }

    }

}
