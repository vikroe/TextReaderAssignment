namespace BigTextReader.Core.Search
{

    public enum SearchPhase { Scanning, Resolving }

    public readonly record struct SearchingProgress(SearchPhase Phase, long Done, long Total, long HitsFound)
    {
        public double Fraction => Total <= 0 ? 0 : (double)Done / Total;
    }
}
