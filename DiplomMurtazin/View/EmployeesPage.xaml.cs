using DiplomMurtazin.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DiplomMurtazin.View
{
    public partial class EmployeesPage : Page
    {
        private EmployeesViewModel _viewModel;

        public EmployeesPage()
        {
            InitializeComponent();
            _viewModel = new EmployeesViewModel();
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

        private void EmployeeCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Employees employee)
            {
                _viewModel.EmployeeClickCommand.Execute(employee);
            }
        }
    }
}