using Microsoft.Win32.SafeHandles;
using System.Buffers;

namespace BigTextReader.Core.Indexing
{
    internal static class LineLocator
    {
        internal static (long Line, long LineStart, long LineEnd) FindLineFromOffset(
            long targetOffset,
            long startLine,
            long startOffset,
            long endOffset,
            SafeFileHandle handle,
            CancellationToken ct)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(Globals.ReadBufferSize);

            try
            {
                long filePos = startOffset, lineNo = startLine, lineStart = startOffset;
                while (filePos < endOffset)
                {
                    ct.ThrowIfCancellationRequested();

                    int read = FileHandleUtils.ReadAt(handle, buffer, filePos, ct);
                    if (read == 0) break;

                    var span = buffer.AsSpan(0, read);
                    int scannedOffset = 0;

                    while (true)
                    {
                        int newLine = span[scannedOffset..].IndexOf((byte)'\n');
                        if (newLine < 0) break;

                        var lineEnd = filePos + scannedOffset + newLine;
                        if (lineEnd >= targetOffset)
                            return (lineNo, lineStart, lineEnd);

                        scannedOffset += newLine + 1;
                        lineStart = filePos + scannedOffset;
                        lineNo++;
                    }

                    filePos += read;
                }
                return (lineNo, lineStart, endOffset - 1); // endOffset is start of next checkpoint
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
