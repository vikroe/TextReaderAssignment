using System.Diagnostics;
using BigTextReader.Core.Sources;

namespace BigTextReader.Core.Loading
{
    public static class FileSaver
    {
        public static async Task SaveFileAsync(
            ILineSource src,
            string dstPath,
            IProgress<TransferProgress>? progress,
            CancellationToken ct)
        {
            if (src is not FileLineSource f)
                throw new NotSupportedException("This source cannot be saved.");

            await CopyFileAsync(f.Path, dstPath, progress, ct);
        }

        private static async Task CopyFileAsync(
            string srcPath,
            string dstPath,
            IProgress<TransferProgress>? progress,
            CancellationToken ct)
        {
            if (string.Equals(Path.GetFullPath(srcPath), Path.GetFullPath(dstPath),
                              StringComparison.OrdinalIgnoreCase))
                throw new IOException("Cannot save over the file that is currently open.");

            await using var srcStream = new FileStream(
                srcPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                Globals.ReadBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var dst = new FileStream(
                dstPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read,
                Globals.ReadBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            long total = srcStream.Length;
            var buffer = new byte[Globals.ReadBufferSize];
            long copied = 0;
            var currentTimestamp = Stopwatch.GetTimestamp();

            int n;
            while ((n = await srcStream.ReadAsync(buffer, ct)) > 0)
            {
                await dst.WriteAsync(buffer.AsMemory(0, n), ct);
                copied += n;

                if (progress is not null && Stopwatch.GetElapsedTime(currentTimestamp).TotalMilliseconds >= Globals.ProgressIntervalMs)
                {
                    progress.Report(new TransferProgress(copied, total));
                    currentTimestamp = Stopwatch.GetTimestamp();
                }
            }

            progress?.Report(new TransferProgress(copied, total));
        }
    }
}
