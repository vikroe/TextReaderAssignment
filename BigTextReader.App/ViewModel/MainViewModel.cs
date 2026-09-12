using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows.Input;
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
            private set => SetBusy(value);
        }

        private void SetBusy(bool value)
        {
            SetField(ref _busy, value);
            CommandManager.InvalidateRequerySuggested();
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
            _tmpStore.Dispose();
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

        private async Task OpenFileAsync(string fileName, long generation, CancellationToken ct)
        {
            if (_generation != generation) return;

            var progress = new Progress<IndexingProgress>(p =>
            {
                if (generation != _generation) return;
                LineCount = p.LinesFound;
                MaxLineBytes = p.MaxLineBytes;
                Progress = p.Fraction;
            });

            var source = new FileLineSource(fileName);
            Source = source;
            BeginPhase("Indexing...", generation); 

            await source.IndexAsync(progress, ct);

            if (generation != _generation) return;
            LineCount = source.LineCount;
            MaxLineBytes = source.MaxLineBytes;
            Progress = 1;
        }

        private IProgress<TransferProgress> TransferHandler(long generation) =>
            new Progress<TransferProgress>(p =>
            {
                if (generation != _generation) return;
                Progress = p.Fraction ?? 0;
                ProgressIndeterminate = p.Fraction is null;
            });

        private void BeginPhase(string status, long generation)
        {
            if (generation != _generation) return;
            StatusText = status;
            Progress = 0;
            ProgressIndeterminate = false;
        }

        private void UpdateStatus(string status, long generation)
        {
            if (generation == _generation)
                StatusText = status;
        }

        public async Task OpenFileCommandAsync(string fileName)
        {
            _cts?.Cancel(); _cts?.Dispose(); _cts = new(); var ct = _cts.Token;
            Busy = true;
            var generation = ++_generation;

            try
            {
                await OpenFileAsync(fileName, generation, ct);
                UpdateStatus("", generation);
            }
            catch (OperationCanceledException) { UpdateStatus("", generation); }
            catch (NotSupportedException ex) { UpdateStatus(ex.Message, generation); }
            catch (IOException) { UpdateStatus("File is already in use", generation);  }
            finally { if (generation == _generation) Busy = false; }
        }

        public async Task OpenUrlCommandAsync(Uri uri)
        {
            _cts?.Cancel(); _cts?.Dispose(); _cts = new(); var ct = _cts.Token;
            Busy = true;

            var generation = ++_generation;
            var progress = TransferHandler(generation);
            BeginPhase("Downloading...", generation);

            try
            {
                string target = _tmpStore.NewFile(".html");
                await UrlDownloader.DownloadAsync(uri, target, progress, ct);
                await OpenFileAsync(target, generation, ct);
                UpdateStatus("", generation);
            } 
            catch (OperationCanceledException) { UpdateStatus("", generation); }
            catch (HttpRequestException ex) { UpdateStatus(ex.Message, generation); }
            catch (IOException ex) { UpdateStatus(ex.Message, generation); }
            catch (UnauthorizedAccessException) { UpdateStatus("No permission to write the temp file.", generation); }
            finally { if (generation == _generation) Busy = false; }
        }

        public async Task SaveFileCommandAsync(string path)
        {
            _cts?.Cancel(); _cts?.Dispose(); _cts = new(); var ct = _cts.Token;
            Busy = true;

            var generation = ++_generation;
            var progress = TransferHandler(generation);
            BeginPhase("Saving...", generation);

            try
            {
                await FileSaver.SaveFileAsync(Source!, path, progress, ct);
                UpdateStatus("", generation);
            }
            catch (NotSupportedException ex) { UpdateStatus(ex.Message, generation); }
            catch (IOException ex) { UpdateStatus(ex.Message, generation); }
            catch (UnauthorizedAccessException) { UpdateStatus("Unauthorized access to target file.", generation); }
            catch (OperationCanceledException) { UpdateStatus("", generation); }
            finally { if (generation == _generation) Busy = false; }
        }

        public async Task GenerateRandomTextCommandAsync(int lineCount, bool lineNumbers)
        {
            _cts?.Cancel(); _cts?.Dispose(); _cts = new(); var ct = _cts.Token;
            Busy = true;

            var generation = ++_generation;
            var progress = TransferHandler(generation);
            BeginPhase("Generating...", generation);

            try
            {
                string target = _tmpStore.NewFile(".txt");
                await RandomTextGenerator.GenerateAsync(target, lineCount, lineNumbers, progress, ct);
                await OpenFileAsync(target, generation, ct);
                UpdateStatus("", generation);
            }
            catch (OperationCanceledException) { UpdateStatus("", generation); }
            catch (IOException ex) { UpdateStatus(ex.Message, generation); }
            catch (UnauthorizedAccessException) { UpdateStatus("No permission to write the temp file.", generation); }
            finally { if (generation == _generation) Busy = false; }
        }
    }
}
