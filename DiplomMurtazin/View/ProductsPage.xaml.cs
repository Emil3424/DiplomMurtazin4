using DiplomMurtazin.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DiplomMurtazin.View
{
    public partial class ProductsPage : Page
    {
        private ProductsViewModel _viewModel;

        public ProductsPage()
        {
            InitializeComponent();
            _viewModel = new ProductsViewModel();
            DataContext = _viewModel;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadedCommand.Execute(null);
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _viewModel.EditCommand.Execute(null);
        }
    }
}