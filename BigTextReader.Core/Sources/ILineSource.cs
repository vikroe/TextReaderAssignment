namespace BigTextReader.Core.Sources
{
    public interface ILineSource: IDisposable
    {
        long LineCount { get; }
        long MaxLineBytes { get; }
        string GetLine(long index);
    }
}
