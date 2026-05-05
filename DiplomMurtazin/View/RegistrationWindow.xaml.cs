using DiplomMurtazin.Core;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DiplomMurtazin.View
{
    public partial class RegistrationWindow : Window
    {
        private bool _isPasswordVisible;
        private bool _isConfirmPasswordVisible;
        private bool _isSyncingPassword;

        public RegistrationWindow()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Сброс сообщений
            ErrorBorder.Visibility = Visibility.Collapsed;
            SuccessBorder.Visibility = Visibility.Collapsed;

            string login = LoginTextBox.Text.Trim();
            string password = _isPasswordVisible ? PasswordTextBox.Text : PasswordBox.Password;
            string confirmPassword = _isConfirmPasswordVisible ? ConfirmPasswordTextBox.Text : ConfirmPasswordBox.Password;
            string role = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Кассир";
            string employeeIdText = EmployeeIdTextBox.Text.Trim();

            // Валидация
            if (string.IsNullOrWhiteSpace(login))
            {
                ShowError("Введите логин");
                return;
            }

            if (login.Length < 3)
            {
                ShowError("Логин должен быть не менее 3 символов");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите пароль");
                return;
            }

            if (password.Length < 3)
            {
                ShowError("Пароль должен быть не менее 3 символов");
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("Пароли не совпадают");
                return;
            }

            int? employeeId = null;
            if (!string.IsNullOrWhiteSpace(employeeIdText))
            {
                if (!int.TryParse(employeeIdText, out int parsedId))
                {
                    ShowError("ID сотрудника должен быть числом");
                    return;
                }
                employeeId = parsedId;
            }

            try
            {
                using (var context = new KPMurtazinEntities())
                {
                    // Проверка существующего логина
                    if (context.Users.Any(u => u.Login == login))
                    {
                        ShowError("Пользователь с таким логином уже существует");
                        return;
                    }

                    // Проверка EmployeeId
                    if (employeeId.HasValue)
                    {
                        var employee = context.Employees.FirstOrDefault(e => e.EmployeeID == employeeId);
                        if (employee == null)
                        {
                            ShowError("Сотрудник с указанным ID не найден");
                            return;
                        }

                        if (context.Users.Any(u => u.EmployeeID == employeeId))
                        {
                            ShowError("Для этого сотрудника уже создана учетная запись");
                            return;
                        }
                    }

                    // Создаем нового пользователя
                    var newUser = new Users
                    {
                        Login = login,
                        Password = password,
                        Role = role,
                        EmployeeID = (int)employeeId,
                        IsActive = true
                    };

                    context.Users.Add(newUser);
                    context.SaveChanges();
                    AuditLogger.Log("CREATE", "User", $"Создан пользователь '{newUser.Login}'", newUser.UserID.ToString(), $"Role={newUser.Role}");

                    // Показываем успех
                    SuccessText.Text = "Регистрация прошла успешно!";
                    SuccessBorder.Visibility = Visibility.Visible;

                    // Очищаем поля
                    LoginTextBox.Text = "";
                    PasswordBox.Password = "";
                    ConfirmPasswordBox.Password = "";
                    PasswordTextBox.Text = "";
                    ConfirmPasswordTextBox.Text = "";
                    EmployeeIdTextBox.Text = "";

                    MessageBox.Show("Регистрация успешна! Теперь вы можете войти.",
                                  "Успех",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);

                    // Закрываем окно
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при регистрации: {ex.Message}");
            }
        }
        private void QuantityTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                InputMask.ApplyQuantityMask(textBox, ShowMaskHint);
                textBox.TextChanged += EmployeeIdTextBox_TextChanged;
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
            UpdateLayout();
        }

        private void ShowMaskHint(string message)
        {
            ShowError($"{message} Пример: 12345");
        }

        private void EmployeeIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = EmployeeIdTextBox.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text) || text.All(char.IsDigit))
            {
                ErrorBorder.Visibility = Visibility.Collapsed;
                ErrorText.Text = string.Empty;
            }
        }

        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            TogglePasswordButton.Content = _isPasswordVisible ? "🙈" : "👁";

            if (_isPasswordVisible)
            {
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordTextBox.Visibility = Visibility.Visible;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Focus();
                PasswordTextBox.CaretIndex = PasswordTextBox.Text.Length;
            }
            else
            {
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordBox.Visibility = Visibility.Visible;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Focus();
            }
        }

        private void ToggleConfirmPasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            _isConfirmPasswordVisible = !_isConfirmPasswordVisible;
            ToggleConfirmPasswordButton.Content = _isConfirmPasswordVisible ? "🙈" : "👁";

            if (_isConfirmPasswordVisible)
            {
                ConfirmPasswordTextBox.Text = ConfirmPasswordBox.Password;
                ConfirmPasswordTextBox.Visibility = Visibility.Visible;
                ConfirmPasswordBox.Visibility = Visibility.Collapsed;
                ConfirmPasswordTextBox.Focus();
                ConfirmPasswordTextBox.CaretIndex = ConfirmPasswordTextBox.Text.Length;
            }
            else
            {
                ConfirmPasswordBox.Password = ConfirmPasswordTextBox.Text;
                ConfirmPasswordBox.Visibility = Visibility.Visible;
                ConfirmPasswordTextBox.Visibility = Visibility.Collapsed;
                ConfirmPasswordBox.Focus();
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncingPassword || !_isPasswordVisible)
            {
                return;
            }

            _isSyncingPassword = true;
            PasswordTextBox.Text = PasswordBox.Password;
            _isSyncingPassword = false;
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncingPassword || !_isConfirmPasswordVisible)
            {
                return;
            }

            _isSyncingPassword = true;
            ConfirmPasswordTextBox.Text = ConfirmPasswordBox.Password;
            _isSyncingPassword = false;
        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingPassword || !_isPasswordVisible)
            {
                return;
            }

            _isSyncingPassword = true;
            PasswordBox.Password = PasswordTextBox.Text;
            _isSyncingPassword = false;
        }

        private void ConfirmPasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingPassword || !_isConfirmPasswordVisible)
            {
                return;
            }

            _isSyncingPassword = true;
            ConfirmPasswordBox.Password = ConfirmPasswordTextBox.Text;
            _isSyncingPassword = false;
        }

        private void CloseErrorButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
            ErrorText.Text = string.Empty;
        }

        private void CloseSuccessButton_Click(object sender, RoutedEventArgs e)
        {
            SuccessBorder.Visibility = Visibility.Collapsed;
            SuccessText.Text = string.Empty;
        }
    }
}