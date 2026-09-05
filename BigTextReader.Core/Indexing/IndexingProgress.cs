namespace BigTextReader.Core.Indexing
{
    public readonly record struct IndexingProgress(long LinesFound, long BytesRead, long TotalBytes)
    {
        public double Fraction => TotalBytes <= 0 ? 0 : (double)BytesRead / TotalBytes;
    }
}
