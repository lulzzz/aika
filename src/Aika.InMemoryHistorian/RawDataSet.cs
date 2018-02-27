using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Aika.Tags;

namespace Aika.Implementations.InMemory {

    /// <summary>
    /// Holds raw tag data for a tag in an <see cref="InMemoryHistorian"/>.
    /// </summary>
    internal class RawDataSet : SortedDictionary<DateTime, TagValue> {

        /// <summary>
        /// Lock for reading data from/writing data to the <see cref="RawDataSet"/>.
        /// </summary>
        public ReaderWriterLockSlim Lock { get; } = new ReaderWriterLockSlim();

    }
}
