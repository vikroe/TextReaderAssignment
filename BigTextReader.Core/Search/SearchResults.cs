namespace BigTextReader.Core.Search
{
    public readonly record struct SearchHit(long ByteOffset, long Line, int Column)
    {
        public const int UnknownColumn = -1;
        public bool HasColumn => Column != UnknownColumn;
    }

    public sealed record SearchResults(SearchHit[] Hits, string Pattern, bool Capped)
    {
        public static readonly SearchResults Empty = new([], "", false);

        public int Count => Hits.Length;
        public bool IsEmpty => Hits.Length == 0;

        public int LowerBound(long line)
        {
            int searchStart = 0;
            int searchEnd = Hits.Length;

            while (searchStart < searchEnd)
            {
                int middle = searchStart + (searchEnd - searchStart) / 2;

                if (Hits[middle].Line < line)
                {
                    searchStart = middle + 1;
                }
                else
                {
                    searchEnd = middle;
                }
            }
            return searchStart;
        }

        public ReadOnlySpan<SearchHit> InLineRange(long firstLine, int lineCount)
        {
            if (lineCount <= 0 || Hits.Length == 0) return default;

            int firstHit = LowerBound(firstLine);
            int afterLastHit = LowerBound(firstLine + lineCount);
            return Hits.AsSpan(firstHit, afterLastHit - firstHit);
        }

        public int NextIndex(int current)
        {
            if (Hits.Length == 0) return -1;
            if (current < 0) return 0;
            return (current + 1) % Hits.Length;
        }

        public int PreviousIndex(int current)
        {
            if (Hits.Length == 0) return -1;
            if (current <= 0) return Hits.Length - 1;
            return current - 1;
        }
    }
}
