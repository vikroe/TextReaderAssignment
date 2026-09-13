using System.Windows.Input;

namespace BigTextReader.App.View
{
    public static class Commands
    {
        public static readonly RoutedUICommand OpenUrl = new(
            "Open URL...",
            nameof(OpenUrl),
            typeof(Commands),
            [new KeyGesture(Key.U, ModifierKeys.Control)]);

        public static readonly RoutedUICommand GenerateRandomText = new(
            "Generate Text...",
            nameof(GenerateRandomText),
            typeof(Commands),
            [new KeyGesture(Key.G, ModifierKeys.Control)]);

        public static readonly RoutedUICommand Search = new("Search", nameof(Search), typeof(Commands));

        public static readonly RoutedUICommand FindNext = new(
            "Find Next",
            nameof(FindNext),
            typeof(Commands),
            [new KeyGesture(Key.F3)]);

        public static readonly RoutedUICommand FindPrevious = new(
            "Find Previous",
            nameof(FindPrevious),
            typeof(Commands),
            [new KeyGesture(Key.F3, ModifierKeys.Shift)]);
    }
}
