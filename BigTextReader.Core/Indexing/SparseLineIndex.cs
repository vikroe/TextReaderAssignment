namespace BigTextReader.Core.Indexing
{
    internal class SparseLineIndex
    {
        private readonly Lock _gate = new();
        private readonly List<long> _checkpoints = [];
        private long _count;
        public long Count => Volatile.Read(ref _count);
        public void SetCount(long count)
        {
            Volatile.Write(ref _count, count);
        }
        private long _maxLineBytes;
        public long MaxLineBytes => Volatile.Read(ref _maxLineBytes);
        public void SetMaxLineBytes(long length)
        {
            Volatile.Write(ref _maxLineBytes, length);
        }

        public void AddCheckpoint(long checkpoint)
        {
            lock (_gate) _checkpoints.Add(checkpoint);
        }

        public (long offset, int skip) Locate(long line)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(line);

            long checkpoint = line / Globals.CheckpointInterval;
            int skip = (int)(line % Globals.CheckpointInterval);

            lock(_gate)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(checkpoint, _checkpoints.Count);
                return (_checkpoints[checked((int)checkpoint)], skip);
            }
        }

        public bool TryGetBlockRange(long line, out long startOffset, out long endOffset)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(line);
            long checkpoint = line / Globals.CheckpointInterval;

            lock(_gate)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(checkpoint, _checkpoints.Count);
                startOffset = _checkpoints[(int)checkpoint];
                if(checkpoint + 1 >= _checkpoints.Count)
                {
                    endOffset = 0;
                    return false;
                }

                endOffset = _checkpoints[(int)checkpoint + 1];
            }

            return true;
        }

        public int CheckpointAtOrBefore(long byteOffset)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(byteOffset);

            lock (_gate)
            {
                var found = _checkpoints.BinarySearch(byteOffset);
                return found >= 0 ? found : Math.Max(0, ~found - 1);
            }
        }
    }
}
