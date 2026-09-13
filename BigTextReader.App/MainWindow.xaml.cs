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

            Closed += (_, _) => _vm.Dispose();
        }

        private async void OnOpenFile(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog(this) == true)
                await _vm.OpenFileCommandAsync(dialog.FileName);
        }

        private async void OnOpenUrl(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new UrlPrompt { Owner = this };
            if (dialog.ShowDialog() == true && dialog.Result is { } url)
                await _vm.OpenUrlCommandAsync(url);
        }
        private async void OnGenerateText(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new RandomTextPrompt { Owner = this };
            if (dialog.ShowDialog() == true)
                await _vm.GenerateRandomTextCommandAsync(dialog.LineCount, dialog.LineNumbers);
        }

        private async void OnSaveFile(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            if (dialog.ShowDialog(this) == true)
                await _vm.SaveFileCommandAsync(dialog.FileName);
        }

        private void OnFind(object sender, ExecutedRoutedEventArgs e) => SearchBar.ShowAndFocus();

        private async void OnSearch(object sender, ExecutedRoutedEventArgs e)
        {
            await _vm.SearchPatternCommandAsync();
        }

        private void OnSearchBarCloseRequested(object? sender, EventArgs e)
        {
            SearchBar.HideBar();
            TextView.Focus();
        }

        // TODO: advance the current hit and ask the view to scroll to it.
        private void OnFindNext(object sender, ExecutedRoutedEventArgs e)
        {
        }

        // TODO: step the current hit backwards and ask the view to scroll to it.
        private void OnFindPrevious(object sender, ExecutedRoutedEventArgs e)
        {
        }

        private void OnCanRun(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void OnCanSave(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = !_vm.Busy && _vm.Source is not null;
        private void OnCanFind(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = _vm.Source is not null;
        private void OnCanSearch(object sender, CanExecuteRoutedEventArgs e) =>
            e.CanExecute = !_vm.Busy && _vm.Source is not null && _vm.SearchText.Length > 0;
        private void OnCanNavigateHits(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = _vm.SearchResultsCount > 0;
    }
}