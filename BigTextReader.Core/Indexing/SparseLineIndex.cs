namespace BigTextReader.Core.Indexing
{
    public class SparseLineIndex
    {
        private readonly Lock _gate = new();
        private readonly List<long> _checkpoints = [];
        private long _count;
        public long Count => Volatile.Read(ref _count);
        internal void SetCount(long count)
        {
            Volatile.Write(ref _count, count);
        }

        internal void AddCheckpoint(long checkpoint)
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
    }
}
