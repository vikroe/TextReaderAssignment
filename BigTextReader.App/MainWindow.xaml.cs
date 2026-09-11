using System.Windows;
using System.Windows.Input;
using BigTextReader.App.ViewModel;
using BigTextReader.App.View.Dialogs;
using Microsoft.Win32;

namespace BigTextReader.App
{
    public partial class MainWindow : Window
    {
        private MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new();
            DataContext = _vm;
        }

        private async void OnOpenFile(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog(this) == true)
                await _vm.OpenFileAsync(dialog.FileName);
        }

        private async void OnOpenUrl(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new UrlPrompt { Owner = this };
            if (dialog.ShowDialog() == true && dialog.Result is { } url)
                await _vm.OpenUrlAsync(url);
        }
        private async void OnGenerateText(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new RandomTextPrompt { Owner = this };
            if (dialog.ShowDialog() == true)
                await _vm.GenerateRandomTextAsync(dialog.LineCount, dialog.LineNumbers);
        }

        private async void OnSaveFile(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            if (dialog.ShowDialog(this) == true)
                await _vm.SaveFileAsync(dialog.FileName);
        }

        private void OnCanRun(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void OnCanSave(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = !_vm.Busy && _vm.Source is not null;
    }
}