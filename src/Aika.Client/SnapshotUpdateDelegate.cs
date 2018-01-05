using System;
using System.Collections.Generic;
using System.Text;
using Aika.Client.Dto;

namespace Aika.Client {
    /// <summary>
    /// Delegate used by <see cref="Clients.ISnapshotSubscriptionClient"/> to push newly-received 
    /// snapshot values to subscribers.
    /// </summary>
    /// <param name="values">The received values.</param>
    public delegate void SnapshotUpdateDelegate(IDictionary<string, TagValueDto[]> values);
}
