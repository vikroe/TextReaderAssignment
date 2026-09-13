using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace BigTextReader.App.View.UserControls
{
    public partial class SearchBar : UserControl
    {
        public SearchBar()
        {
            InitializeComponent();
        }

        public event EventHandler? CloseRequested;

        public void ShowAndFocus()
        {
            Visibility = Visibility.Visible;

            Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
            {
                PatternInput.Focus();
                PatternInput.SelectAll();
            });
        }

        public void HideBar() => Visibility = Visibility.Collapsed;

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;

            CloseRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }

        private void OnCloseClick(object sender, RoutedEventArgs e) =>
            CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
