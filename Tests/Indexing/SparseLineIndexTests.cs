using System.Collections.Concurrent;
using BigTextReader.Core;
using BigTextReader.Core.Indexing;

namespace Tests.Indexing;

public class SparseLineIndexTests
{
    private const int Interval = Globals.CheckpointInterval;

    private static long OffsetOf(long k) => k * 12345 + 3;

    private static SparseLineIndex WithCheckpoints(int count)
    {
        var index = new SparseLineIndex();
        for (int k = 0; k < count; k++) index.AddCheckpoint(OffsetOf(k));
        index.SetCount((long)count * Interval);
        return index;
    }


    [Fact]
    public void NewIndex_IsEmpty()
    {
        var index = new SparseLineIndex();

        Assert.Equal(0, index.Count);
        Assert.Equal(0, index.MaxLineBytes);
    }

    [Fact]
    public void Locate_OnEmptyIndex_Throws()
    {
        var index = new SparseLineIndex();

        Assert.Throws<ArgumentOutOfRangeException>(() => index.Locate(0));
    }

    [Fact]
    public void Locate_FirstLine_ReturnsFirstCheckpointWithNoSkip()
    {
        var index = WithCheckpoints(1);

        Assert.Equal((OffsetOf(0), 0), index.Locate(0));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(Interval - 1)]
    public void Locate_WithinFirstInterval_SkipsFromFirstCheckpoint(long line)
    {
        var index = WithCheckpoints(1);

        Assert.Equal((OffsetOf(0), (int)line), index.Locate(line));
    }

    [Fact]
    public void Locate_ExactIntervalBoundary_UsesTheNextCheckpointWithNoSkip()
    {
        var index = WithCheckpoints(2);

        Assert.Equal((OffsetOf(1), 0), index.Locate(Interval));
    }

    [Fact]
    public void Locate_JustPastBoundary_SkipsFromTheNextCheckpoint()
    {
        var index = WithCheckpoints(2);

        Assert.Equal((OffsetOf(1), 3), index.Locate(Interval + 3));
    }

    [Fact]
    public void Locate_AcrossManyCheckpoints_MapsEveryLineToItsOwnCheckpoint()
    {
        const int checkpoints = 50;
        var index = WithCheckpoints(checkpoints);

        for (long k = 0; k < checkpoints; k++)
        {
            Assert.Equal((OffsetOf(k), 0), index.Locate(k * Interval));
            Assert.Equal((OffsetOf(k), Interval - 1), index.Locate(k * Interval + Interval - 1));
        }
    }

    [Fact]
    public void Locate_LastIndexedLine_Succeeds()
    {
        const int checkpoints = 3;
        var index = WithCheckpoints(checkpoints);
        long lastLine = (long)checkpoints * Interval - 1;

        Assert.Equal((OffsetOf(checkpoints - 1), Interval - 1), index.Locate(lastLine));
    }


    [Fact]
    public void Locate_OnePastTheIndexedRange_Throws()
    {
        var index = WithCheckpoints(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => index.Locate(Interval));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-Interval)]
    [InlineData(long.MinValue)]
    public void Locate_NegativeLine_Throws(long line)
    {
        var index = WithCheckpoints(4);
        Assert.Throws<ArgumentOutOfRangeException>(() => index.Locate(line));
    }

    [Fact]
    public void SetCount_IsVisibleThroughCount()
    {
        var index = new SparseLineIndex();

        index.SetCount(42);
        Assert.Equal(42, index.Count);

        index.SetCount(1_000_000);
        Assert.Equal(1_000_000, index.Count);
    }

    [Fact]
    public void SetMaxLineBytes_IsVisibleThroughMaxLineBytes()
    {
        var index = new SparseLineIndex();

        index.SetMaxLineBytes(4096);

        Assert.Equal(4096, index.MaxLineBytes);
    }

    [Fact]
    public void Count_IsIndependentOfCheckpointCount()
    {
        var index = new SparseLineIndex();
        index.AddCheckpoint(0);

        index.SetCount(37);

        Assert.Equal(37, index.Count);
        Assert.Equal((0L, 36), index.Locate(36));
    }

    [Fact]
    public async Task ReaderNeverSeesACountItCannotLocate()
    {
        const int checkpoints = 50_000;
        var index = new SparseLineIndex();
        var failures = new ConcurrentQueue<string>();

        var writer = Task.Run(() =>
        {
            for (int k = 0; k < checkpoints; k++)
            {
                index.AddCheckpoint(OffsetOf(k));
                index.SetCount((k + 1L) * Interval);
            }
        });

        var reader = Task.Run(() =>
        {
            while (!writer.IsCompleted)
            {
                long count = index.Count;
                if (count == 0) continue;

                long line = count - 1;
                var (offset, skip) = index.Locate(line);

                if (offset != OffsetOf(line / Interval))
                    failures.Enqueue($"line {line}: offset {offset}, expected {OffsetOf(line / Interval)}");
                if (skip != (int)(line % Interval))
                    failures.Enqueue($"line {line}: skip {skip}, expected {line % Interval}");
            }
        });

        await Task.WhenAll(writer, reader);

        Assert.Empty(failures);
        Assert.Equal((long)checkpoints * Interval, index.Count);
    }
}
