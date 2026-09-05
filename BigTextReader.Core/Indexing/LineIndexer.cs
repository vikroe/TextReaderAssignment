using Microsoft.Win32.SafeHandles;
using System.Buffers;
using System.Diagnostics;
using BigTextReader.Core.Text;

namespace BigTextReader.Core.Indexing
{
    public static class LineIndexer
    {
        public static long Scan(
            SafeFileHandle handle,
            long fileLength,
            SparseLineIndex index,
            EncodingDetector.BomInfo bomInfo,
            IProgress<IndexingProgress>? progress,
            CancellationToken ct = default)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(Globals.ReadBufferSize);

            try
            {
                long filePos = bomInfo.Length, lineNo = 0, lineStart = bomInfo.Length;
                index.AddCheckpoint(filePos);

                var currentTimestamp = Stopwatch.GetTimestamp();
                while (filePos < fileLength)
                {
                    ct.ThrowIfCancellationRequested();

                    int read = RandomAccess.Read(handle, buffer, filePos);
                    if (read == 0) break;

                    var span = buffer.AsSpan(0, read);
                    int scannedOffset = 0;

                    while (true)
                    {
                        int newLine = span[scannedOffset..].IndexOf((byte)'\n');
                        if (newLine < 0) break;

                        scannedOffset += newLine + 1;
                        lineNo++;
                        lineStart = filePos + scannedOffset;

                        if (lineNo % Globals.CheckpointInterval == 0)
                            index.AddCheckpoint(filePos + scannedOffset);
                    }

                    filePos += read;
                    if (Stopwatch.GetElapsedTime(currentTimestamp).TotalMilliseconds > Globals.ProgressIntervalMs)
                    {
                        index.SetCount(lineNo);
                        progress?.Report(new(lineNo, filePos, fileLength));
                        currentTimestamp = Stopwatch.GetTimestamp();
                    }
                }

                if (lineStart < filePos) lineNo++;

                index.SetCount(lineNo);
                progress?.Report(new(lineNo, fileLength, fileLength));
                return lineNo;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            
        }
    }

}
