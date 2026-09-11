namespace BigTextReader.Core.Loading
{
    public readonly record struct TransferProgress(long Done, long? Total)
    {
        public double? Fraction => Total is > 0 ? (double)Done / Total : null;
    }
}
