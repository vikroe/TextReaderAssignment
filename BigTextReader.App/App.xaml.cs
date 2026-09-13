using System.Windows;
using System.Windows.Threading;

namespace BigTextReader.App
{
    public partial class App : Application
    {
        private string? _lastReportedError;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            string message = $"{e.Exception.GetType().Name}: {e.Exception.Message}";
            if (message != _lastReportedError)
            {
                _lastReportedError = message;
                MessageBox.Show(
                    message,
                    "BigTextReader",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            e.Handled = true;
        }
    }
}
