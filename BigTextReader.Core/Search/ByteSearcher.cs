
using BigTextReader.Core.Loading;
using BigTextReader.Core.Text;
using Microsoft.Win32.SafeHandles;
using System.Buffers;
using System.Diagnostics;
using System.Text;

namespace BigTextReader.Core.Search
{
    internal static class ByteSearcher
    {
        internal static (List<long>, bool) Search(
            string pattern,
            SafeFileHandle handle,
            long fileLength,
            EncodingDetector.BomInfo bomInfo,
            IProgress<SearchingProgress>? progress,
            CancellationToken ct = default)
        {
            byte[] bytePattern = Encoding.UTF8.GetBytes(pattern);
            if (bytePattern.Length == 0 || bytePattern.Length >= Globals.ReadBufferSize)
                return ([], false);

            List<long> hits = [];
            byte[] buffer = new byte[Globals.ReadBufferSize];

            long filePos = bomInfo.Length;
            long found = 0;

            var currentTimestamp = Stopwatch.GetTimestamp();
            while (filePos < fileLength && found < Globals.MaxSearchHits)
            {
                ct.ThrowIfCancellationRequested();

                int read = FileHandleUtils.ReadAt(handle, buffer, filePos, ct);
                if (read < bytePattern.Length) break;

                var span = buffer.AsSpan(0, read);
                int scannedOffset = 0;

                while (found < Globals.MaxSearchHits)
                {
                    int newHit = span[scannedOffset..].IndexOf(bytePattern);
                    if (newHit < 0) break;
                    hits.Add(filePos + scannedOffset + newHit);
                    scannedOffset += newHit + 1;
                    found++;
                }


                filePos += read;
                if (filePos < fileLength)
                    filePos -= bytePattern.Length - 1;
                if (Stopwatch.GetElapsedTime(currentTimestamp).TotalMilliseconds > Globals.ProgressIntervalMs)
                {
                    progress?.Report(new(SearchPhase.Scanning, filePos, fileLength, found));
                    currentTimestamp = Stopwatch.GetTimestamp();
                }
            }

            progress?.Report(new(SearchPhase.Scanning, fileLength, fileLength, found));

            return (hits, found >= Globals.MaxSearchHits);
        }
    }
}
