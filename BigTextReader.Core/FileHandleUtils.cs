using Microsoft.Win32.SafeHandles;

namespace BigTextReader.Core
{
    internal static class FileHandleUtils
    {
        public static int FillRead(SafeFileHandle handle, Span<byte> buffer, long offset, CancellationToken ct = default)
        {
            int totalRead = 0;
            while (totalRead < buffer.Length)
            {
                int read = ReadAt(handle, buffer[totalRead..], offset + totalRead, ct);
                if (read == 0) break;
                totalRead += read;
            }
            return totalRead;
        }

        public static int ReadAt(SafeFileHandle handle, Span<byte> buffer, long fileOffset, CancellationToken ct)
        {
            try { return RandomAccess.Read(handle, buffer, fileOffset); }
            catch (ObjectDisposedException) { throw new OperationCanceledException(ct); }
        }
    }
}
