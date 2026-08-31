using System;
using System.Collections.Generic;
using System.Text;

namespace BigTextReader.Core.Sources
{
    public interface ILineSource
    {
        long LineCount { get; }
        string GetLine(long index);
    }
}
