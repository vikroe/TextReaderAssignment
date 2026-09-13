namespace BigTextReader.Core.Caching
{
    internal class CacheBlock
    {
        public long LastUsed { get; private set; }
        private readonly List<string> _lines;
        private long _firstLine;
        public int Count => _lines.Count;

        public CacheBlock()
        {
            _lines = [];
            LastUsed = _firstLine = -1;
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
            LastUsed = tick;
        }

        public void AddLine(string line) => _lines.Add(line);

        public void Touch(long tick)
        {
            LastUsed = tick;
        }
    }
}
