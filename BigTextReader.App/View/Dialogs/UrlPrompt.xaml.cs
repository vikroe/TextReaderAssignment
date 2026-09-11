using System.Windows;
using System.Windows.Controls;

namespace BigTextReader.App.View.Dialogs
{
    public partial class UrlPrompt : Window
    {
        private Uri? _uri;
        public Uri? Result => _uri;

        public UrlPrompt()
        {
            InitializeComponent();
        }

        private static (Uri? Uri, string Error) Parse(string text)
        {
            text = text.Trim();
            if (text.Length == 0) return (null, "");   // don't nag before anything is typed

            if (!text.Contains("://", StringComparison.Ordinal))
            {
                int colon = text.IndexOf(':');
                if (colon > 0 && (colon + 1 >= text.Length || !char.IsAsciiDigit(text[colon + 1])))
                    return (null, $"Only http and https are supported (got \"{text[..colon]}\").");

                text = "https://" + text;
            }

            if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
                return (null, "Not a valid address.");

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return (null, $"Only http and https are supported (got \"{uri.Scheme}\").");

            return (uri, "");
        }

        private void OnAddressChanged(object sender, TextChangedEventArgs e)
        {
            (_uri, string error) = Parse(AddressBox.Text);
            OpenButton.IsEnabled = _uri is not null;
            ErrorText.Text = error;
        }

        private void OnOpen(object sender, RoutedEventArgs e)
        {
            if (_uri is null) return;
            DialogResult = true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AddressBox.Focus();
            AddressBox.SelectAll();
        }
    }
}
