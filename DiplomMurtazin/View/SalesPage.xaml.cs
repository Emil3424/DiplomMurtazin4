using DiplomMurtazin.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DiplomMurtazin.View
{
    public partial class SalesPage : Page
    {
        private SalesViewModel _viewModel;

        public SalesPage()
        {
            InitializeComponent();
            _viewModel = new SalesViewModel();
            DataContext = _viewModel;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadedCommand.Execute(null);
        }

        private void ProductDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _viewModel.AddToSaleCommand.Execute(null);
        }
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _viewModel.CheckStockAvailability();

        }
    }
}