using BigTextReader.Core.Indexing;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace BigTextReader.Core.Sources
{
    public sealed class FileLineSource : ILineSource
    {
        private readonly SafeFileHandle _handle;
        private readonly SparseLineIndex _index = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly long _fileLength;
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
        }

        public long LineCount => _index.Count;

        public Task<long> IndexAsync(IProgress<IndexingProgress>? progress, CancellationToken ct = default)
        {
            var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ct);
            return Task.Run(() =>
            {
                try { return LineIndexer.Scan(_handle, _fileLength, _index, progress, linked.Token); }
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
