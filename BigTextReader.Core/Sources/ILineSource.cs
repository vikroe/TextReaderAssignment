namespace BigTextReader.Core.Sources
{
    public interface ILineSource: IDisposable
    {
        long LineCount { get; }
        string GetLine(long index);
    }
}
