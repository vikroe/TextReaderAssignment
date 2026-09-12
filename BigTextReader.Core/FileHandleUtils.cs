using Microsoft.Win32.SafeHandles;

namespace BigTextReader.Core
{
    internal static class FileHandleUtils
    {
        public static int FillRead(SafeFileHandle handle, Span<byte> buffer, long offset)
        {
            int total = 0;
            while (total < buffer.Length)
            {
                int n = RandomAccess.Read(handle, buffer[total..], offset + total);
                if (n == 0) break;
                total += n;
            }
            return total;
        }

        public static int ReadAt(SafeFileHandle handle, Span<byte> buffer, long fileOffset, CancellationToken ct)
        {
            try { return RandomAccess.Read(handle, buffer, fileOffset); }
            catch (ObjectDisposedException) { throw new OperationCanceledException(ct); }
        }
    }
}
