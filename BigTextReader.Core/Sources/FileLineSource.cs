using BigTextReader.Core.Indexing;
using BigTextReader.Core.Text;
using Microsoft.Win32.SafeHandles;

namespace BigTextReader.Core.Sources
{
    public sealed class FileLineSource : ILineSource
    {
        private readonly SafeFileHandle _handle;
        private readonly SparseLineIndex _index = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly long _fileLength;
        private readonly EncodingDetector.BomInfo _bom;
        private int _disposed;

        public FileLineSource (string path)
        {
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
                throw new NotSupportedException("Unsupported BOM - only UTF-8 is supported");
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

        public string GetLine(long index)
        {
            throw new NotImplementedException();
        }
    }
}
