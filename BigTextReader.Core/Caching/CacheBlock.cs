namespace BigTextReader.Core.Caching
{
    internal class CacheBlock
    {
        public long LastUsed;
        public List<string> Lines;
        public long FirstLine;
        public long Bytes;

        internal CacheBlock()
        {
            Lines = [];
            LastUsed = FirstLine = -1;
            Bytes = 0;
        }

        internal bool TryGetLine(long lineIndex, out string line)
        {
            line = "";
            if (FirstLine == -1) return false;

            if (lineIndex >= FirstLine && lineIndex < FirstLine + Lines.Count)
            {
                line = Lines[(int)(lineIndex - FirstLine)];
                return true;
            }
            return false;
        }
    }
}
