using BigTextReader.Core.Indexing;
using BigTextReader.Core.Text;
using Microsoft.Win32.SafeHandles;
using System.Buffers;
using System.Diagnostics;

namespace BigTextReader.Core.Search
{
    internal static class HitResolver
    {
        private static bool IsInRange(long value, long start, long end)
        {
            return value >= start && value <= end;
        }

        public static SearchResults ResolveHitOffsets(
            List<long> hits,
            SafeFileHandle handle,
            long fileLength,
            string pattern,
            bool capped,
            SparseLineIndex index,
            IProgress<SearchingProgress>? progress,
            CancellationToken ct)
        {
            SearchHit[] searchHits = new SearchHit[hits.Count];
            byte[] lineBuffer = new byte[Globals.MaxRenderedLineLength];

            long line = 0, start = -1, end = -1;
            int column = 0;
            Span<byte> lineSpan = [];
            var currentTimestamp = Stopwatch.GetTimestamp();
            for (var i = 0; i < hits.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var hit = hits[i];

                if (!IsInRange(hit, start, end))
                {
                    var checkpointId = index.CheckpointAtOrBefore(hit);
                    if (!index.TryGetBlockRange(
                        Globals.CheckpointInterval * checkpointId,
                        out var checkpointStart,
                        out var checkpointEnd))
                    {
                        checkpointEnd = fileLength;
                    }

                    (line, start, end) = LineLocator.FindLineFromOffset(
                        hit,
                        checkpointId * Globals.CheckpointInterval,
                        checkpointStart,
                        checkpointEnd,
                        handle,
                        ct);

                    var want = (int)Math.Max(0, Math.Min(end - start, Globals.MaxRenderedLineLength));
                    lineSpan = lineBuffer.AsSpan(0, want);
                    var read = FileHandleUtils.FillRead(handle, lineSpan, start);
                    if (read != want)
                        throw new IOException("A line was shorter than expected");

                    column = LineDecoder.DecodeLineUpTo(lineSpan, hit - start);
                }
                else
                {
                    column = LineDecoder.DecodeLineUpTo(lineSpan, hit - start, (int)(hits[i - 1] - start), searchHits[i - 1].Column);
                }

                searchHits[i] = new(hit, line, column); 
                
                if (Stopwatch.GetElapsedTime(currentTimestamp).TotalMilliseconds > Globals.ProgressIntervalMs)
                {
                    progress?.Report(new(SearchPhase.Resolving, i, searchHits.Length, searchHits.Length));
                    currentTimestamp = Stopwatch.GetTimestamp();
                }
            }

            progress?.Report(new (SearchPhase.Resolving, searchHits.Length, searchHits.Length, searchHits.Length));
            return new SearchResults(searchHits, pattern, capped);
        }
    }
}
