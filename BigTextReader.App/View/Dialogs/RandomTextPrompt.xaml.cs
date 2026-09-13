using System.Windows;
using System.Windows.Controls;

namespace BigTextReader.App.View.Dialogs
{
    public partial class RandomTextPrompt : Window
    {
        private int _lineCount;
        public int LineCount => _lineCount;
        public bool LineNumbers => LineNumberCheckBox.IsChecked == true;

        public RandomTextPrompt()
        {
            InitializeComponent();
        }

        private void OnLineCountChanged(object sender, TextChangedEventArgs e)
        {
            if(!Int32.TryParse(LineCountBox.Text, out _lineCount))
            {
                ErrorText.Text = "Only input a valid integer.";
            }
            else if(_lineCount > 1_000_000)
            {
                ErrorText.Text = "Please input a number less than or equal to 1M";
            }
            else if (_lineCount < 0)
            {
                ErrorText.Text = "Please input a positive number";
            }
            else
            {
                ErrorText.Text = "";
                GenerateButton.IsEnabled = true;
                return;
            }
            GenerateButton.IsEnabled = false;
        }

        private void OnGenerate(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LineCountBox.Focus();
            LineCountBox.Text = "1000000";
            LineCountBox.SelectAll();
        }
    }
}
