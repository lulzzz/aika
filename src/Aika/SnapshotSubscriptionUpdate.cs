using System;
using System.Collections.Generic;
using System.Text;
using Aika.Tags;

namespace Aika {
    /// <summary>
    /// Delegate for emitting new values from a <see cref="SnapshotSubscription"/>.
    /// </summary>
    /// <param name="values">
    /// The emitted values, indexed by tag name.
    /// </param>
    public delegate void SnapshotSubscriptionUpdate(IDictionary<string, IEnumerable<TagValue>> values);
}
