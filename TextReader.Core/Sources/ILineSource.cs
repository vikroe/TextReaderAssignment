using System;
using System.Collections.Generic;
using System.Text;

namespace TextReader.Core.Sources
{
    public interface ILineSource
    {
        long LineCount { get; }
        string GetLine(long index);
    }
}
