using DiplomMurtazin.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace DiplomMurtazin.View
{
    public partial class UniversalEditPage : Page
    {
        private UniversalEditViewModel _viewModel;

        public UniversalEditPage()
        {
            InitializeComponent();
            _viewModel = new UniversalEditViewModel();
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

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.AddNewItem();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите запись для удаления", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show("Удалить выбранную запись?",
                                       "Подтверждение",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _viewModel.DeleteSelectedItem(MainDataGrid.SelectedItem);
            }
        }
    }
}