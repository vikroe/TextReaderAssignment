using System.ComponentModel;
using System.Runtime.CompilerServices;
using BigTextReader.Core.Sources;

namespace BigTextReader.App.ViewModel
{
    public sealed class MainViewModel : IDisposable, INotifyPropertyChanged
    {
        public MainViewModel()
        {
            SelectSynthSourceCommand = new RelayCommand(
                execute => SelectSyntheticSource(),
                canExecute => { return true; });
        }

        private ILineSource? _source; 
        public ILineSource? Source
        {
            get => _source;
            private set => SetField(ref _source, value);
        }

        public RelayCommand SelectSynthSourceCommand { get; }

        public void Dispose()
        {
            throw new NotImplementedException();
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
    }
}
