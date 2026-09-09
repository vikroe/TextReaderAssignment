using System.Text;
using BigTextReader.Core;
using BigTextReader.Core.Indexing;
using BigTextReader.Core.Text;

namespace Tests.Indexing;

public sealed class LineIndexerTests : IDisposable
{
    private readonly TempFiles _files = new();
    public void Dispose() => _files.Dispose();

    private const int Interval = Globals.CheckpointInterval;

    private const int LineWidth = 16;

    private static byte[] FixedWidthLines(int count) =>
        Encoding.UTF8.GetBytes(string.Concat(
            Enumerable.Range(0, count).Select(i => $"{i:D10} abcd\n")));

    private static (long Lines, SparseLineIndex Index) Scan(
        string path,
        IProgress<IndexingProgress>? progress = null,
        CancellationToken ct = default)
    {
        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        long length = RandomAccess.GetLength(handle);

        Span<byte> head = stackalloc byte[4];
        int read = RandomAccess.Read(handle, head, 0);
        var bom = EncodingDetector.Detect(head[..read]);

        var index = new SparseLineIndex();
        long lines = LineIndexer.Scan(handle, length, index, bom, progress, ct);
        return (lines, index);
    }

    [Theory]
    [InlineData("", 0)]                 // empty file
    [InlineData("\n", 1)]               // a single empty line
    [InlineData("a", 1)]                // no newline anywhere — still one line
    [InlineData("a\n", 1)]              // terminated: no phantom extra line
    [InlineData("a\nb", 2)]             // unterminated trailing line is counted
    [InlineData("a\nb\n", 2)]
    [InlineData("\n\n\n", 3)]           // three empty lines
    [InlineData("a\r\nb\r\n", 2)]       // CRLF terminators
    public void LineCount_IsCorrect(string content, long expected)
    {
        var (lines, index) = Scan(_files.Write("t.txt", content));

        Assert.Equal(expected, lines);
        Assert.Equal(expected, index.Count);
    }

    [Fact]
    public void NoNewlinesAtAll_IsExactlyOneLine()
    {
        var (lines, index) = Scan(_files.Write("blob.txt", new byte[8 << 20]));

        Assert.Equal(1, lines);
        Assert.Equal(8 << 20, index.MaxLineBytes);
    }

    [Fact]
    public void NewlineExactlyOnReadBufferBoundary_IsNotMissed()
    {
        var first = new byte[Globals.ReadBufferSize];
        Array.Fill(first, (byte)'a');
        first[^1] = (byte)'\n';

        var (lines, index) = Scan(_files.Write("boundary.txt", [.. first, .. "second\n"u8]));

        Assert.Equal(2, lines);
        Assert.Equal(Globals.ReadBufferSize - 1, index.MaxLineBytes);
    }

    [Fact]
    public void LineOverlappingABuffer_IsMeasuredInFull()
    {
        const int length = Globals.ReadBufferSize + 12_345;
        var line = new byte[length];
        Array.Fill(line, (byte)'x');

        var (lines, index) = Scan(_files.Write("overlap.txt", [.. line, .. "\n"u8]));

        Assert.Equal(1, lines);
        Assert.Equal(length, index.MaxLineBytes);
    }

    [Fact]
    public void MaxLineBytes_IsTheLongestLine()
    {
        var (_, index) = Scan(_files.Write("varied.txt", "ab\nabcdefgh\nabcd\n"));

        Assert.Equal(8, index.MaxLineBytes);
    }

    [Fact]
    public void MaxLineBytes_CountsTheCarriageReturnOnCrlf()
    {
        var (_, index) = Scan(_files.Write("crlf.txt", "abc\r\n"));

        Assert.Equal(4, index.MaxLineBytes);
    }

    [Fact]
    public void Utf8Bom_IsSkipped_AndTheFirstCheckpointPointsPastIt()
    {
        var (lines, index) = Scan(_files.Write("bom.txt", [.. "﻿"u8, .. "first\nsecond\n"u8]));

        Assert.Equal(2, lines);
        Assert.Equal((3L, 0), index.Locate(0));
    }

    [Fact]
    public void FileContainingOnlyABom_HasNoLines()
    {
        var (lines, index) = Scan(_files.Write("bomonly.txt", "﻿"u8));

        Assert.Equal(0, lines);
        Assert.Equal(0, index.MaxLineBytes);
    }

    [Fact]
    public void Checkpoints_LandOnEveryIntervalthLine()
    {
        const int lines = Interval * 3 + 7;
        var (count, index) = Scan(_files.Write("many.txt", FixedWidthLines(lines)));

        Assert.Equal(lines, count);

        for (long k = 0; k <= 3; k++)
            Assert.Equal((k * Interval * LineWidth, 0), index.Locate(k * Interval));

        Assert.Equal((2 * Interval * LineWidth, 55), index.Locate(2 * Interval + 55));
    }

    [Fact]
    public void FewerLinesThanTheInterval_StillHasCheckpointZero()
    {
        var (lines, index) = Scan(_files.Write("short.txt", FixedWidthLines(10)));

        Assert.Equal(10, lines);
        Assert.Equal((0L, 7), index.Locate(7));
    }

    // ---- progress ------------------------------------------------------------------------

    [Fact]
    public void Progress_FinalReportIsComplete()
    {
        var samples = new List<IndexingProgress>();
        string path = _files.Write("p.txt", FixedWidthLines(5_000));

        var (lines, index) = Scan(path, new SyncProgress<IndexingProgress>(samples.Add));

        var last = samples[^1];
        Assert.Equal(lines, last.LinesFound);
        Assert.Equal(index.MaxLineBytes, last.MaxLineBytes);
        Assert.Equal(last.TotalBytes, last.BytesRead);
        Assert.Equal(1.0, last.Fraction);
    }

    [Fact]
    public void Progress_IsOptional()
    {
        var (lines, _) = Scan(_files.Write("np.txt", FixedWidthLines(100)), progress: null);

        Assert.Equal(100, lines);
    }

    [Fact]
    public void AlreadyCancelledToken_ThrowsBeforeReadingAnything()
    {
        string path = _files.Write("c.txt", FixedWidthLines(200_000));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => Scan(path, null, cts.Token));
    }

    [Fact]
    public void EmptyFile_IgnoresCancellation_BecauseTheLoopNeverRuns()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var (lines, _) = Scan(_files.Write("empty.txt", ""), null, cts.Token);

        Assert.Equal(0, lines);
    }
}
