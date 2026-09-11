using System.Diagnostics;
using System.Text;

namespace BigTextReader.Core.Loading
{
    public static class RandomTextGenerator
    {
        private static readonly string[] Words =
        [
            "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit",
            "sed", "do", "eiusmod", "tempor", "incididunt", "ut", "labore", "et", "dolore",
            "magna", "aliqua", "enim", "ad", "minim", "veniam", "quis", "nostrud", "ullamco",
            "exercitation", "laboris", "nisi", "aliquip", "ex", "ea", "commodo", "consequat",
            "duis", "aute", "irure", "in", "reprehenderit", "voluptate", "velit", "esse",
            "cillum", "eu", "fugiat", "nulla", "pariatur", "excepteur", "sint", "occaecat",
            "cupidatat", "non", "proident", "sunt", "culpa", "qui", "officia", "deserunt",
            "mollit", "anim", "id", "est", "laborum",
        ];

        public static Task GenerateAsync(
            string path,
            int lineCount,
            bool lineNumbers,
            IProgress<TransferProgress>? progress,
            CancellationToken ct)
            => Task.Run(() => Generate(path, lineCount, lineNumbers, progress, ct), ct);

        private static void Generate(
            string path,
            int lineCount,
            bool lineNumbers,
            IProgress<TransferProgress>? progress,
            CancellationToken ct)
        {
            using var stream = new FileStream(
                path, FileMode.Create, FileAccess.Write, FileShare.Read,
                Globals.ReadBufferSize, FileOptions.SequentialScan);

            using var writer = new StreamWriter(stream, new UTF8Encoding(false), Globals.ReadBufferSize);

            var rng = new Random();
            var stringBuilder = new StringBuilder(512);
            var currentTimestamp = Stopwatch.GetTimestamp();

            for (int i = 0; i < lineCount; i++)
            {
                ct.ThrowIfCancellationRequested();

                stringBuilder.Clear();

                if (lineNumbers) stringBuilder.Append(i.ToString("D10")).Append(' ');

                int words = rng.Next(100) == 0 ? rng.Next(100, 400) : rng.Next(1, 30);
                for (int w = 0; w < words; w++)
                {
                    if (w > 0) stringBuilder.Append(' ');
                    stringBuilder.Append(Words[rng.Next(Words.Length)]);
                }

                writer.Write(stringBuilder);
                writer.Write('\n');

                if (progress is not null && Stopwatch.GetElapsedTime(currentTimestamp).TotalMilliseconds >= Globals.ProgressIntervalMs)
                {
                    progress.Report(new TransferProgress(i + 1, lineCount));
                    currentTimestamp = Stopwatch.GetTimestamp();
                }
            }

            writer.Flush();
            progress?.Report(new TransferProgress(lineCount, lineCount));
        }
    }
}
