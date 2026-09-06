using Microsoft.Win32.SafeHandles;
using System.Buffers;
using System.Diagnostics;
using BigTextReader.Core.Text;

namespace BigTextReader.Core.Indexing
{
    internal static class LineIndexer
    {
        internal static long Scan(
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
                long filePos = bomInfo.Length, lineNo = 0, lineStart = bomInfo.Length, maxLineBytes = 0;
                index.AddCheckpoint(filePos);


                var currentTimestamp = Stopwatch.GetTimestamp();
                while (filePos < fileLength)
                {
                    ct.ThrowIfCancellationRequested();

                    try { read = RandomAccess.Read(handle, buffer, filePos); }
                    catch (ObjectDisposedException) { throw new OperationCanceledException(ct); }

                    var span = buffer.AsSpan(0, read);
                    int scannedOffset = 0;

                    while (true)
                    {
                        int newLine = span[scannedOffset..].IndexOf((byte)'\n');
                        if (newLine < 0) break;

                        long lineEnd = filePos + scannedOffset + newLine;
                        if (lineEnd - lineStart > maxLineBytes) maxLineBytes = lineEnd - lineStart;
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
                        index.SetMaxLineBytes(maxLineBytes);
                        progress?.Report(new(lineNo, maxLineBytes, filePos, fileLength));
                        currentTimestamp = Stopwatch.GetTimestamp();
                    }
                }

                if (lineStart < filePos)
                {
                    lineNo++;
                    if (filePos - lineStart > maxLineBytes) maxLineBytes = filePos - lineStart;
                }

                index.SetCount(lineNo);
                index.SetMaxLineBytes(maxLineBytes);
                progress?.Report(new(lineNo, maxLineBytes, fileLength, fileLength));
                return lineNo;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            
        }
    }

}
