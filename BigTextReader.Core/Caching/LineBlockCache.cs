using BigTextReader.Core.Text;
using Microsoft.Win32.SafeHandles;

namespace BigTextReader.Core.Caching
{
    internal sealed class LineBlockCache
    {
        private readonly CacheBlock[] _blocks = new CacheBlock[Globals.CacheBlockCount];
        private long _tick = 0;
        private readonly byte[] _buffer = new byte[Globals.MaxBlockBytes];

        public LineBlockCache()
        {
            for(var i = 0; i < Globals.CacheBlockCount; i++)
            {
                _blocks[i] = new();
            }
        }

        public bool TryGetBlock(long lineIndex, out CacheBlock? block)
        {
            foreach (var b in _blocks)
            {
                if (b.TryGetLine(lineIndex, out var line))
                {
                    block = b;
                    b.LastUsed = ++_tick;
                    return true;
                }
            }

            block = null;
            return false;
        }

        private int FindLastUsedBlock()
        {
            long lastUsed = long.MaxValue;
            int lastUsedBlock = -1;
            for (var i = 0; i < _blocks.Length; i++)
            {
                var block = _blocks[i];
                if (block.LastUsed == -1)
                {
                    return i;
                }
                if (block.LastUsed < lastUsed)
                {
                    lastUsed = block.LastUsed;
                    lastUsedBlock = i;
                }
            }
            return lastUsedBlock;
        }

        public CacheBlock LoadBlock(
            SafeFileHandle handle,
            long start,
            long end,
            long firstLine
            )
        {
            var block = _blocks[FindLastUsedBlock()];
            block.Lines.Clear();
            block.FirstLine = firstLine;
            block.Bytes = 0;
            block.LastUsed = ++_tick;

            int want = (int)Math.Min(Globals.MaxBlockBytes, end - start);
            int read = FillRead(handle, _buffer.AsSpan(0, want), start);
            var span = _buffer.AsSpan(0, read);
            int scannedOffset = 0;

            var lineNo = 0;
            while (lineNo < Globals.CheckpointInterval)
            {
                int newLine = span[scannedOffset..].IndexOf((byte)'\n');
                if (newLine < 0) break;

                var lineEnd = newLine + scannedOffset;
                block.Lines.Add(LineDecoder.DecodeByteLine(span[scannedOffset..lineEnd]));
                block.Bytes += newLine;

                scannedOffset += newLine + 1;
                lineNo++;
            }

            var truncated = want < end - start;
            if (!truncated && scannedOffset < span.Length && block.Lines.Count < Globals.CheckpointInterval)
                block.Lines.Add(LineDecoder.DecodeByteLine(span[scannedOffset..])); // Add trailing line if present

            return block;
        }

        private static int FillRead(SafeFileHandle handle, Span<byte> buffer, long offset)
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
    }
}
