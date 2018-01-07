using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aika {

    /// <summary>
    /// Interface that marks a class as a back-end data historian.
    /// </summary>
    /// <seealso cref="ITagSearch"/>
    /// <seealso cref="ITagDataReader"/>
    /// <seealso cref="ITagDataWriter"/>
    /// <seealso cref="ITagManager"/>
    public interface IHistorian : ITagSearch, ITagManager, ITagDataReader, ITagDataWriter {

        /// <summary>
        /// Indicates if the <see cref="IHistorian"/> has finished initializing.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets a description for the <see cref="IHistorian"/> implementation.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets bespoke properties associated with the <see cref="IHistorian"/> implementation.
        /// </summary>
        IDictionary<string, object> Properties { get; }

        /// <summary>
        /// Initializes the <see cref="IHistorian"/>.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel initialization.</param>
        /// <returns>
        /// A task that will initialize the historian.
        /// </returns>
        Task Init(CancellationToken cancellationToken);

    }

}
