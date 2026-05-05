using DiplomMurtazin.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace DiplomMurtazin.View
{
    public partial class Torg12Page : Page
    {
        private readonly Torg12ViewModel _viewModel;

        public Torg12Page()
        {
            InitializeComponent();
            _viewModel = new Torg12ViewModel();
            DataContext = _viewModel;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadedCommand.Execute(null);
        }

        private void ProductsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _viewModel.AddItemCommand.Execute(null);
        }
    }
}

