using System.Windows;
using TextReader.App.ViewModel;

namespace TextReader.App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainViewModel vm = new MainViewModel();
            DataContext = vm;
        }
    }
}