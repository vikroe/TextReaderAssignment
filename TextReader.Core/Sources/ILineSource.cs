using System;
using System.Collections.Generic;
using System.Text;

namespace TextReader.Core.Sources
{
    internal interface ILineSource
    {
        long LineCount { get; }
        string GetLine(long index);
    }
}
