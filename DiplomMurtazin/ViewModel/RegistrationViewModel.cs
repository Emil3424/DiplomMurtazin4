using DiplomMurtazin.Core;
using DiplomMurtazin.Model;
using DiplomMurtazin.View;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace DiplomMurtazin.ViewModel
{
    public class RegistrationViewModel : BaseViewModel
    {
        private string _login;
        private string _password;
        private string _confirmPassword;
        private string _role = "Кассир";
        private int? _employeeId;
        private string _errorMessage;
        private string _successMessage;

        public string Login
        {
            get => _login;
            set => Set(ref _login, value);
        }

        public string Password
        {
            get => _password;
            set => Set(ref _password, value);
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => Set(ref _confirmPassword, value);
        }

        public string Role
        {
            get => _role;
            set => Set(ref _role, value);
        }

        public int? EmployeeId
        {
            get => _employeeId;
            set => Set(ref _employeeId, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => Set(ref _errorMessage, value);
        }

        public string SuccessMessage
        {
            get => _successMessage;
            set => Set(ref _successMessage, value);
        }

        public ICommand RegisterCommand { get; }
        public ICommand CancelCommand { get; }

        public RegistrationViewModel()
        {
            RegisterCommand = new RelayCommand(ExecuteRegister, CanExecuteRegister);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        private bool CanExecuteRegister(object parameter)
        {
            string password = parameter as string;
            return !string.IsNullOrWhiteSpace(Login) &&
                   !string.IsNullOrWhiteSpace(password) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword);
        }

        private void ExecuteRegister(object parameter)
        {
            // Сброс сообщений
            ErrorMessage = "";
            SuccessMessage = "";

            string password = parameter.ToString();

            // Валидация
            if (password != ConfirmPassword)
            {
                ErrorMessage = "Пароли не совпадают.";
                return;
            }

            if (password.Length < 3)
            {
                ErrorMessage = "Пароль должен быть не менее 3 символов.";
                return;
            }

            try
            {
                using (var context = DataContext.GetContext())
                {
                    // Проверка существующего логина
                    if (context.Users.Any(u => u.Login == Login))
                    {
                        ErrorMessage = "Пользователь с таким логином уже существует.";
                        return;
                    }

                    // Проверка EmployeeId
                    if (EmployeeId.HasValue)
                    {
                        var employee = context.Employees.FirstOrDefault(e => e.EmployeeID == EmployeeId);
                        if (employee == null)
                        {
                            ErrorMessage = "Сотрудник с указанным ID не найден.";
                            return;
                        }

                        if (context.Users.Any(u => u.EmployeeID == EmployeeId))
                        {
                            ErrorMessage = "Для этого сотрудника уже создана учетная запись.";
                            return;
                        }
                    }

                    // Создаем нового пользователя
                    var newUser = new Users
                    {
                        Login = Login,
                        Password = password, // Простой пароль
                        Role = Role,
                        EmployeeID = (int)EmployeeId,
                        IsActive = true
                    };

                    context.Users.Add(newUser);
                    context.SaveChanges();

                    SuccessMessage = "Регистрация прошла успешно!";

                    // Очищаем поля
                    Login = "";
                    Password = "";
                    ConfirmPassword = "";
                    EmployeeId = null;

                    // Показываем сообщение и закрываем окно через 2 секунды
                    MessageBox.Show("Регистрация успешна! Теперь вы можете войти.",
                                  "Успех",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);

                    CloseWindow();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при регистрации: {ex.Message}";
            }
        }

        private void ExecuteCancel(object parameter)
        {
            CloseWindow();
        }

        private void CloseWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is RegistrationWindow)
                {
                    window.Close();
                    break;
                }
            }
        }
    }
}