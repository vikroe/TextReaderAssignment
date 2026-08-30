using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using TextReader.Core.Sources;

namespace TextReader.App.ViewModel
{
    public sealed class MainViewModel : IDisposable
    {
        private ILineSource? _source; 

        public ILineSource? Source
        {
            get => _source;
            private set => SetField(ref _source, value);
        }

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
    }
}
