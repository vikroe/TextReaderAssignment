namespace BigTextReader.Core.Caching
{
    internal class CacheBlock
    {
        public long LastUsed { get; private set; }
        private readonly List<string> _lines;
        private long _firstLine;
        private long _bytes;
        public int Count => _lines.Count;

        public CacheBlock()
        {
            _lines = [];
            LastUsed = _firstLine = -1;
            _bytes = 0;
        }

        public bool TryGetLine(long lineIndex, out string line)
        {
            line = "";
            if (_firstLine == -1) return false;

            if (lineIndex >= _firstLine && lineIndex < _firstLine + _lines.Count)
            {
                line = _lines[(int)(lineIndex - _firstLine)];
                return true;
            }
            return false;
        }

        public void Reset(long firstLine, long tick)
        {
            _lines.Clear();
            _firstLine = firstLine;
            _bytes = 0;
            LastUsed = tick;
        }

        public void AddLine(string line, long bytes)
        {
            _lines.Add(line);
            _bytes += bytes;
        }

        public void Touch(long tick)
        {
            LastUsed = tick;
        }
    }
}
