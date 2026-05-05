using DiplomMurtazin.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace DiplomMurtazin.View
{
    public partial class HistoryPage : Page
    {
        private readonly HistoryViewModel _viewModel;

        public HistoryPage()
        {
            InitializeComponent();
            _viewModel = new HistoryViewModel();
            DataContext = _viewModel;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadCommand.Execute(null);
        }
    }
}
