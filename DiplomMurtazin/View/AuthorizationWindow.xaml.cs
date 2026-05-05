using System;
using System.Linq;
using System.Windows;

namespace DiplomMurtazin.View
{
    public partial class AuthorizationWindow : Window
    {
        private bool _isPasswordVisible;
        private bool _isSyncingPassword;

        public AuthorizationWindow()
        {
            InitializeComponent();
            LoginTextBox.Focus();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = _isPasswordVisible ? PasswordTextBox.Text : PasswordBox.Password;

            // Валидация
            if (string.IsNullOrWhiteSpace(login))
            {
                ShowError("Введите логин");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите пароль");
                return;
            }

            try
            {
                using (var context = new KPMurtazinEntities())
                {
                    var user = context.Users
                        .FirstOrDefault(u => u.Login == login &&
                                            u.Password == password &&
                                            u.IsActive == true);

                    if (user != null)
                    {
                        App.CurrentUser = user;

                        var mainWindow = new MainWindow();
                        mainWindow.Show();

                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window is AuthorizationWindow)
                            {
                                window.Close();
                                break;
                            }
                        }
                    }
                    else
                    {
                        ShowError("Неверный логин или пароль.");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка подключения: {ex.Message}");
            }
        }

        private void RegistrationLink_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var registrationWindow = new RegistrationWindow();
            registrationWindow.Owner = this;
            registrationWindow.ShowDialog();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
            UpdateLayout();
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

        private void PasswordTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_isSyncingPassword || !_isPasswordVisible)
            {
                return;
            }

            _isSyncingPassword = true;
            PasswordBox.Password = PasswordTextBox.Text;
            _isSyncingPassword = false;
        }

        private void CloseErrorButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
            ErrorText.Text = string.Empty;
        }
    }
}