using BigTextReader.Core.Search;

namespace BigTextReader.Core.Sources
{
    public interface ILineSource: IDisposable
    {
        long LineCount { get; }
        long MaxLineBytes { get; }
        string GetLine(long index);
        Task<SearchResults> SearchAsync(string pattern, IProgress<SearchingProgress>? progress, CancellationToken ct);
    }
}
