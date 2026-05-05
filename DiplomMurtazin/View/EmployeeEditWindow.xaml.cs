using DiplomMurtazin.Core;
using DiplomMurtazin.ViewModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DiplomMurtazin.View
{
    public partial class EmployeeEditWindow : Window
    {
        private EmployeeEditViewModel _viewModel;

        public EmployeeEditWindow() : this(null) { }

        public EmployeeEditWindow(Employees employee = null)
        {
            InitializeComponent();
            _viewModel = new EmployeeEditViewModel(employee);
            DataContext = _viewModel;
        }

        public Employees GetEmployee()
        {
            return _viewModel.Employee;
        }

        private void ShowMaskHint(string message)
        {
            _viewModel.ErrorMessage = message;
            UpdateLayout();
        }

        private void TryHideMaskHint(TextBox textBox)
        {
            if (textBox == null)
            {
                return;
            }

            string text = textBox.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text) || text.All(char.IsDigit) || text.All(c => char.IsDigit(c) || c == ' '))
            {
                _viewModel.ErrorMessage = string.Empty;
            }
        }

        private void PhoneTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
                InputMask.ApplyPhoneMask(textBox);
        }

        private void InnTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
                InputMask.ApplyInnMask(textBox, false); // false - физлицо
        }

        private void PassportTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                InputMask.ApplyPassportMask(textBox, ShowMaskHint);
                textBox.TextChanged += (s, args) => TryHideMaskHint(textBox);
            }
        }

        private void CloseErrorButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ErrorMessage = string.Empty;
            ErrorBorder.Visibility = Visibility.Collapsed;
        }
    }
}