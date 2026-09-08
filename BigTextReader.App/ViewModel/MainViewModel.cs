using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using BigTextReader.Core.Indexing;
using BigTextReader.Core.Sources;
using Microsoft.Win32;

namespace BigTextReader.App.ViewModel
{
    public sealed class MainViewModel : IDisposable, INotifyPropertyChanged
    {
        public MainViewModel()
        {
            SelectSynthSourceCommand = new RelayCommand(
                execute => SelectSyntheticSource(),
                canExecute => { return true; });
            SelectFileSourceCommand = new RelayCommand(
                execute => _ = SelectFileSource(),
                canExecute => { return true; });
        }

        private CancellationTokenSource? _cts;
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

        public RelayCommand SelectSynthSourceCommand { get; }
        public RelayCommand SelectFileSourceCommand { get; }

        public void Dispose()
        {
            Source?.Dispose();
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

        public void SelectSyntheticSource()
        {
            Source = new SyntheticSource();
        }

        public async Task SelectFileSource()
        {
            _cts?.Cancel(); _cts?.Dispose(); _cts = new(); var ct = _cts.Token;

            OpenFileDialog fileDialog = new OpenFileDialog();
            bool? success = fileDialog.ShowDialog();

            if (success == true)
            {
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
                    var source = new FileLineSource(fileDialog.FileName);
                    Source = source;
                    await source.IndexAsync(progress, ct);
                    LineCount = source.LineCount;
                    MaxLineBytes = source.MaxLineBytes;
                    Progress = 1;
                }
                catch (OperationCanceledException) { }
                catch (NotSupportedException ex) { StatusText = ex.Message; }
                catch (IOException) { StatusText = "File is already in use"; }
                finally { Busy = false; }
            }
        }
    }
}
