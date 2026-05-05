using DiplomMurtazin.Core;
using DiplomMurtazin.Model;
using DiplomMurtazin.View;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace DiplomMurtazin.ViewModel
{
    public class AuthorizationViewModel : BaseViewModel
    {
        private string _login;
        private string _errorMessage;

        public string Login
        {
            get => _login;
            set => Set(ref _login, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => Set(ref _errorMessage, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand OpenRegistrationCommand { get; }

        public AuthorizationViewModel()
        {
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
            OpenRegistrationCommand = new RelayCommand(ExecuteOpenRegistration);
        }

        private bool CanExecuteLogin(object parameter)
        {
            string password = parameter as string;
            return !string.IsNullOrWhiteSpace(Login) &&
                   !string.IsNullOrWhiteSpace(password);
        }

        private void ExecuteLogin(object parameter)
        {
            try
            {
                string password = parameter.ToString();

                using (var context = DataContext.GetContext())
                {
                    var user = context.Users
                        .FirstOrDefault(u => u.Login == Login &&
                                            u.Password == password &&
                                            u.IsActive == true);

                    if (user != null)
                    {
                        // Сохраняем пользователя в глобальном классе
                        App.CurrentUser = user;

                        MessageBox.Show($"Добро пожаловать, {user.Login}!",
                                      "Успешный вход",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Information);

                        // Открываем главное окно (создайте его позже)
                        // var mainWindow = new MainWindow();
                        // mainWindow.Show();

                        // Закрываем текущее окно
                        CloseCurrentWindow();
                    }
                    else
                    {
                        ErrorMessage = "Неверный логин или пароль.";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка подключения: {ex.Message}";
            }
        }

        private void ExecuteOpenRegistration(object parameter)
        {
            var registrationWindow = new RegistrationWindow();
            registrationWindow.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            registrationWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            registrationWindow.ShowDialog();
        }

        private void CloseCurrentWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is AuthorizationWindow)
                {
                    window.Close();
                    break;
                }
            }
        }
    }
}