using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using BigTextReader.Core.Indexing;
using BigTextReader.Core.Loading;
using BigTextReader.Core.Sources;

namespace BigTextReader.App.ViewModel
{
    public sealed class MainViewModel : IDisposable, INotifyPropertyChanged
    {
        private CancellationTokenSource? _cts;
        private readonly TempFileStore _tmpStore = new();
        private ILineSource? _source; 
        public ILineSource? Source
        {
            get => _source;
            private set => SetSource(value);
        }

        private void SetSource(ILineSource? next)
        {
            var old = Source;
            SetField(ref _source, next, nameof(Source));
            LineCount = next?.LineCount ?? 0;
            MaxLineBytes = next?.MaxLineBytes ?? 0;
            StatusText = "";
            old?.Dispose();
        }

        private long _lineCount;
        public long LineCount
        {
            get => _lineCount;
            private set => SetField(ref _lineCount, value);
        }

        private long _maxLineBytes;

        public long MaxLineBytes
        {
            get => _maxLineBytes;
            private set => SetField(ref _maxLineBytes, value);
        }

        private double _progress;
        public double Progress
        {
            get => _progress;
            private set => SetField(ref _progress, value);
        }

        private bool _progressIndeterminate;
        public bool ProgressIndeterminate
        {
            get => _progressIndeterminate;
            private set => SetField(ref _progressIndeterminate, value);
        }

        private long _generation;
        private bool _busy;
        public bool Busy
        {
            get => _busy;
            private set => SetField(ref _busy, value);
        }

        private string _statusText = "";
        public string StatusText
        {
            get => _statusText;
            private set => SetField(ref _statusText, value);
        }

        public void Dispose()
        {
            Source?.Dispose();
            _cts?.Dispose();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        public async Task OpenFileAsync(string fileName)
        {
            _cts?.Cancel(); _cts?.Dispose(); _cts = new(); var ct = _cts.Token;

            var progress = new Progress<IndexingProgress>(p =>
            {
                if (ct.IsCancellationRequested) return;
                LineCount = p.LinesFound;
                MaxLineBytes = p.MaxLineBytes;
                Progress = p.Fraction;
            });
            Busy = true;
            Progress = 0;

            try
            {
                var source = new FileLineSource(fileName);
                Source = source;
                StatusText = "Indexing...";
                await source.IndexAsync(progress, ct);
                LineCount = source.LineCount;
                MaxLineBytes = source.MaxLineBytes;
                Progress = 1;
                StatusText = "";
            }
            catch (OperationCanceledException) { StatusText = ""; }
            catch (NotSupportedException ex) { StatusText = ex.Message; }
            catch (IOException) { StatusText = "File is already in use"; }
            finally { Busy = false; }
        }

        public async Task OpenUrlAsync(Uri? uri)
        {
            if (uri is null)
                return; // should not happen

            _cts?.Cancel(); _cts?.Dispose(); _cts = new(); var ct = _cts.Token;

            var progress = new Progress<TransferProgress>(p =>
            {
                if (ct.IsCancellationRequested) return;
                Progress = p.Fraction ?? 0;
                ProgressIndeterminate = p.Fraction is null;
            });
            Busy = true;
            Progress = 0;

            try
            {
                StatusText = "Downloading...";
                string target = _tmpStore.NewFile(".html");
                await UrlDownloader.DownloadAsync(uri, target, progress, ct);
                Progress = 1;
                await OpenFileAsync(target);
            } 
            catch (OperationCanceledException) { StatusText = ""; }
            catch (HttpRequestException ex) { StatusText = ex.Message; }
            catch (IOException ex) { StatusText = ex.Message; }
            catch (UnauthorizedAccessException) { StatusText = "No permission to write the temp file."; }
            finally { Busy = false; }
        }

        public async Task SaveFileAsync(string path)
        {
            if (Source is null)
                return; // should not happen

            _cts?.Cancel(); _cts?.Dispose(); _cts = new(); var ct = _cts.Token;

            var progress = new Progress<TransferProgress>(p =>
            {
                if (ct.IsCancellationRequested) return;
                Progress = p.Fraction ?? 0;
                ProgressIndeterminate = p.Fraction is null;
            });
            Busy = true;
            Progress = 0;

            try
            {
                StatusText = "Saving...";
                await FileSaver.SaveFileAsync(Source, path, progress, ct);
                Progress = 1;
                StatusText = "";
            }
            catch (NotSupportedException ex) { StatusText = ex.Message; }
            catch (IOException ex) { StatusText = ex.Message; }
            catch (UnauthorizedAccessException) { StatusText = "Unauthorized access to target file."; }
            catch (OperationCanceledException) { StatusText = ""; }
            finally { Busy = false; }
        }

        public async Task GenerateRandomTextAsync(int lineCount, bool lineNumbers)
        {
            _cts?.Cancel(); _cts?.Dispose(); _cts = new(); var ct = _cts.Token;

            var progress = new Progress<TransferProgress>(p =>
            {
                if (ct.IsCancellationRequested) return;
                Progress = p.Fraction ?? 0;
            });
            Busy = true;
            Progress = 0;

            try
            {
                StatusText = "Generating...";
                string target = _tmpStore.NewFile(".txt");
                await RandomTextGenerator.GenerateAsync(target, lineCount, lineNumbers, progress, ct);
                await OpenFileAsync(target);
            }
            catch (OperationCanceledException) { StatusText = ""; }
            catch (IOException ex) { StatusText = ex.Message; }
            catch (UnauthorizedAccessException) { StatusText = "No permission to write the temp file."; }
            finally { Busy = false; }
        }
    }
}
