using DiplomMurtazin.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace DiplomMurtazin.View
{
    public partial class ViewSettingsPage : Page
    {
        private ViewSettingsViewModel _viewModel;

        public ViewSettingsPage()
        {
            InitializeComponent();
            _viewModel = new ViewSettingsViewModel();
            DataContext = _viewModel;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadedCommand.Execute(null);
        }

        private void TablesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Обновляем статус
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.DisposeContext();
        }
    }
}