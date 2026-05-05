using DiplomMurtazin.ViewModel;
using System.Windows;

namespace DiplomMurtazin.View
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel(MainFrame);
            DataContext = _viewModel;
        }
    }
}