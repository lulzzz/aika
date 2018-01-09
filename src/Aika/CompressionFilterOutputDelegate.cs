using System;
using System.Collections.Generic;
using System.Text;

namespace Aika
{
    public delegate void CompressionFilterOutputDelegate(TagValue[] valuesToArchive, TagValue nextArchiveCandidate);
}
