using DiplomMurtazin.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace DiplomMurtazin.View
{
    public partial class ReportsPage : Page
    {
        private ReportsViewModel _viewModel;

        public ReportsPage()
        {
            InitializeComponent();
            _viewModel = new ReportsViewModel();
            DataContext = _viewModel;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadedCommand.Execute(null);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.DisposeContext();
        }
    }
}