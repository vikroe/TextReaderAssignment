using System.Diagnostics;

namespace BigTextReader.Core.Loading
{
    public static class UrlDownloader
    {
        private static readonly HttpClient _client = new()
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };

        static UrlDownloader()
        {
            _client.DefaultRequestHeaders.UserAgent.ParseAdd("BigTextReader/1.0");
        }

        public static async Task DownloadAsync(
            Uri url,
            string path,
            IProgress<TransferProgress>? progress,
            CancellationToken ct)
        {
            using var response = await _client.GetAsync(url,
                HttpCompletionOption.ResponseHeadersRead,
                ct);
            response.EnsureSuccessStatusCode();

            long? total = response.Content.Headers.ContentLength;
            await using var src = await response.Content.ReadAsStreamAsync(ct);
            await using var dst = new FileStream(
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read,
                Globals.ReadBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var buffer = new byte[Globals.ReadBufferSize];
            long written = 0;
            var currentTimestamp = Stopwatch.GetTimestamp();

            int n;
            while((n = await src.ReadAsync(buffer, ct)) > 0)
            {
                await dst.WriteAsync(buffer.AsMemory(0, n), ct);
                written += n;

                if (progress != null && Stopwatch.GetElapsedTime(currentTimestamp).TotalMilliseconds >= Globals.ProgressIntervalMs)
                {
                    progress?.Report(new(written, total));
                    currentTimestamp = Stopwatch.GetTimestamp();
                }
            }

            progress?.Report(new(written, total));
        }
    }
}
