using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aika.Client.Clients {

    /// <summary>
    /// Describes a client that receives snapshot value changes from Aika via a persistent connection.
    /// </summary>
    public interface ISnapshotSubscriptionClient : IDisposable {

        /// <summary>
        /// Gets a flag indicating if the client is currently connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Raised whenever the <see cref="ISnapshotSubscriptionClient"/> receives new values from 
        /// Aika.
        /// </summary>
        event SnapshotUpdateDelegate ValuesReceived;


        /// <summary>
        /// Subscribes to the specified tag names.
        /// </summary>
        /// <param name="tagNames">The tags to subscribe to.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will perform the subscription.
        /// </returns>
        Task Subscribe(IEnumerable<string> tagNames, CancellationToken cancellationToken);


        /// <summary>
        /// Unsubscribes from the specified tag names.
        /// </summary>
        /// <param name="tagNames">The tag names to unsubscribe from.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will perform the unsubscription.
        /// </returns>
        Task Unsubscribe(IEnumerable<string> tagNames, CancellationToken cancellationToken);


        /// <summary>
        /// Starts the persistent connection.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will start the connection.
        /// </returns>
        Task Start(CancellationToken cancellationToken);


        /// <summary>
        /// Stops the persistent connection.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will stop the connection.
        /// </returns>
        Task Stop(CancellationToken cancellationToken);

    }
}
