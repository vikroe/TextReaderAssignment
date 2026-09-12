using BigTextReader.Core.Caching;
using BigTextReader.Core.Indexing;
using BigTextReader.Core.Loading;
using BigTextReader.Core.Search;
using BigTextReader.Core.Text;
using Microsoft.Win32.SafeHandles;

namespace BigTextReader.Core.Sources
{
    public sealed class FileLineSource : ILineSource
    {
        private readonly string _path;
        public string Path => _path;
        private readonly SafeFileHandle _handle;
        private readonly SparseLineIndex _index = new();
        private readonly LineBlockCache _cache = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly long _fileLength;
        private readonly EncodingDetector.BomInfo _bom;
        private int _disposed;

        public FileLineSource(string path)
        {
            _path = path;
            _handle = File.OpenHandle(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                FileOptions.RandomAccess);
            _fileLength = RandomAccess.GetLength(_handle);

            Span<byte> head = stackalloc byte[4];
            var read = RandomAccess.Read(_handle, head, 0);
            _bom = EncodingDetector.Detect(head[..read]);

            if (!_bom.Supported)
            {
                throw new NotSupportedException("Unsupported BOM - only ASCII/UTF-8 is supported");
            }
        }

        public long LineCount => _index.Count;
        public long MaxLineBytes => _index.MaxLineBytes;

        public Task<long> IndexAsync(
            IProgress<IndexingProgress>? progress,
            CancellationToken ct = default)
        {
            var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ct);
            return Task.Run(() =>
            {
                try
                {
                    return LineIndexer.Scan(
                        _handle,
                        _fileLength,
                        _index,
                        _bom,
                        progress,
                        linked.Token);
                }
                finally { linked.Dispose(); }
            }, linked.Token);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _cts.Cancel();
            _cts.Dispose();
            _handle.Dispose();
        }

        public string GetLine(long lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= _index.Count) return "";

            if (!_cache.TryGetBlock(lineIndex, out var block))
            {
                long firstLine = lineIndex / Globals.CheckpointInterval * Globals.CheckpointInterval;
                var gotRange = _index.TryGetBlockRange(lineIndex, out var start, out var end);
                if (!gotRange) end = _fileLength;
                block = _cache.LoadBlock(_handle, start, end, firstLine);
            }

            if (block == null || !block.TryGetLine(lineIndex, out var line))
            {
                return "";
            }
            return line;
        }

        public Task<SearchResults> SearchAsync(string pattern, IProgress<SearchingProgress>? progress, CancellationToken ct)
        {
            var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ct);
            return Task.Run(() =>
            {
                try
                {
                    var (offsets, capped) = ByteSearcher.Search(
                        pattern,
                        _handle,
                        _fileLength,
                        _bom,
                        progress,
                        linked.Token);
                    return HitResolver.ResolveHitOffsets(
                        offsets,
                        _handle,
                        _fileLength,
                        pattern,
                        capped,
                        _index,
                        progress,
                        linked.Token);
                }
                finally { linked.Dispose(); }
            }, linked.Token);
        }
    }
}
