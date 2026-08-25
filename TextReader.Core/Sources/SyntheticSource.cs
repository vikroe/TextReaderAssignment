using System;
using System.Collections.Generic;
using System.Text;

namespace TextReader.Core.Sources
{
    internal class SyntheticSource: ILineSource
    {
        public long LineCount => 500_000_000;
        public string GetLine(long index)
        {
            return $"Line {index}: " + new string('x', (int)(index % 120));
        }
    }
}
