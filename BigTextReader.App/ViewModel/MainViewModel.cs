using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Windows.Input;
using BigTextReader.Core.Indexing;
using BigTextReader.Core.Loading;
using BigTextReader.Core.Search;
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
            SearchResults = SearchResults.Empty;
            _resultsPattern = "";
            CurrentSearchResult = -1;
            CommandManager.InvalidateRequerySuggested();
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
            SetField(ref _busy, value, nameof(Busy));
            CommandManager.InvalidateRequerySuggested();
        }

        private string _statusText = "";
        public string StatusText
        {
            get => _statusText;
            private set => SetField(ref _statusText, value);
        }

        private SearchResults _searchResults = SearchResults.Empty;
        public SearchResults SearchResults
        {
            get => _searchResults;
            private set
            {
                if (!SetField(ref _searchResults, value)) return;
                OnPropertyChanged(nameof(SearchResultsCount));
                OnPropertyChanged(nameof(SearchStatus));
            }
        }

        private int _currentSearchResult = -1;
        public int CurrentSearchResult
        {
            get => _currentSearchResult;
            private set
            {
                if (!SetField(ref _currentSearchResult, value)) return;
                OnPropertyChanged(nameof(SearchStatus));
            }
        }
        public int SearchResultsCount => _searchResults.Count;

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (!SetField(ref _searchText, value)) return;
                OnPropertyChanged(nameof(SearchStatus));
            }
        }
        private string _resultsPattern = "";

        public string SearchStatus
        {
            get
            {
                if (_resultsPattern.Length == 0) return "";
                if (_searchText != _resultsPattern) return "";
                if (_searchResults.IsEmpty) return "No results";

                string total = _searchResults.Capped
                    ? $"{_searchResults.Count:N0}+"
                    : $"{_searchResults.Count:N0}";

                return _currentSearchResult < 0
                    ? $"{total} matches"
                    : $"{_currentSearchResult + 1:N0} of {total}";
            }
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

        public Task OpenFileCommandAsync(string fileName) =>
            RunAsync("Indexing...", async (generation, ct) =>
                await OpenFileAsync(fileName, generation, ct));

        public Task OpenUrlCommandAsync(Uri uri) =>
            RunAsync("Downloading...", async (generation, ct) =>
            {
                var progress = TransferHandler(generation);
                string target = _tmpStore.NewFile(".html");
                await UrlDownloader.DownloadAsync(uri, target, progress, ct);
                await OpenFileAsync(target, generation, ct);
            });

        public Task SaveFileCommandAsync(string path) =>
            RunAsync("Saving...", async (generation, ct) =>
            {
                var progress = TransferHandler(generation);
                await FileSaver.SaveFileAsync(Source!, path, progress, ct);
            });

        public Task GenerateRandomTextCommandAsync(int lineCount, bool lineNumbers) =>
            RunAsync("Generating...", async (generation, ct) =>
            {
                var progress = TransferHandler(generation);
                string target = _tmpStore.NewFile(".txt");
                await RandomTextGenerator.GenerateAsync(target, lineCount, lineNumbers, progress, ct);
                await OpenFileAsync(target, generation, ct);
            });

        private IProgress<SearchingProgress> SearchHandler(long generation) =>
            new Progress<SearchingProgress>(p =>
            {
                if (generation != _generation) return;
                StatusText = p.Phase == SearchPhase.Scanning ? "Scanning..." : "Indexing results...";
                Progress = p.Fraction;
            });

        public Task SearchPatternCommandAsync() =>
            RunAsync("Searching...", async (generation, ct) =>
            {
                var progress = SearchHandler(generation);
                var pattern = SearchText;
                var results = await Source!.SearchAsync(pattern, progress, ct);

                if (generation != _generation) return;
                SearchResults = results;
                _resultsPattern = pattern;

                CurrentSearchResult = -1;
                OnPropertyChanged(nameof(SearchStatus));
            });
        
        public event EventHandler<SearchHit>? ScrollToHitRequested;

        public void GoToNextHit() => MoveToHit(_searchResults.NextIndex(_currentSearchResult));

        public void GoToPreviousHit() => MoveToHit(_searchResults.PreviousIndex(_currentSearchResult));

        private void MoveToHit(int index)
        {
            if (index < 0) return;

            CurrentSearchResult = index;
            ScrollToHitRequested?.Invoke(this, _searchResults.Hits[index]);
        }

        private static string? Describe(Exception ex) => ex switch
        {
            OperationCanceledException => "",
            NotSupportedException or HttpRequestException or IOException => ex.Message,
            UnauthorizedAccessException => "No permission to access that file.",
            _ => null,
        };

        private async Task RunAsync(string phase, Func<long, CancellationToken, Task> work)
        {
            _cts?.Cancel(); _cts?.Dispose(); _cts = new();
            var ct = _cts.Token;
            var generation = ++_generation;

            Busy = true;
            BeginPhase(phase, generation);

            try
            {
                await work(generation, ct);
                UpdateStatus("", generation);
            }
            catch (Exception ex) when (Describe(ex) is not null)
            {
                UpdateStatus(Describe(ex)!, generation);
            }
            finally { if (generation == _generation) Busy = false; }
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
    }
}
