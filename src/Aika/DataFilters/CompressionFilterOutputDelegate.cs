using System;
using System.Collections.Generic;
using System.Text;
using Aika.Tags;

namespace Aika.DataFilters {
    public delegate void CompressionFilterOutputDelegate(TagValue[] valuesToArchive, TagValue nextArchiveCandidate);
}
