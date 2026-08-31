using System.Windows;
using BigTextReader.App.ViewModel;

namespace BigTextReader.App
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