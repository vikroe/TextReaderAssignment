using System.Windows.Input;

namespace BigTextReader.App.View
{
    public static class Commands
    {
        public static readonly RoutedUICommand OpenUrl = new("Open URL...", nameof(OpenUrl), typeof(Commands));
        public static readonly RoutedUICommand GenerateRandomText = new("Generate Text...", nameof(GenerateRandomText), typeof(Commands));
    }
}
