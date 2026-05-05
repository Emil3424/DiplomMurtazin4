using DiplomMurtazin.Core;
using DiplomMurtazin.View;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DiplomMurtazin.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        private bool _isSalesSelected;
        private bool _isProductsSelected;
        private Users _currentUser;
        private Frame _mainFrame;

        public bool IsSalesSelected
        {
            get => _isSalesSelected;
            set => Set(ref _isSalesSelected, value);
        }

        public bool IsProductsSelected
        {
            get => _isProductsSelected;
            set => Set(ref _isProductsSelected, value);
        }

        public Users CurrentUser
        {
            get => _currentUser;
            set => Set(ref _currentUser, value);
        }

        public ICommand NavigateCommand { get; }
        public ICommand LogoutCommand { get; }

        public MainViewModel(Frame mainFrame)
        {
            _mainFrame = mainFrame;
            CurrentUser = App.CurrentUser;
            NavigateCommand = new RelayCommand(Navigate);
            LogoutCommand = new RelayCommand(Logout);

            // По умолчанию открываем страницу продаж
            Navigate("Sales");
        }

        private void Navigate(object parameter)
        {
            string pageName = parameter as string;

            switch (pageName)
            {
                case "Sales":
                    _mainFrame.Navigate(new SalesPage());
                    IsSalesSelected = true;
                    IsProductsSelected = false;
                    break;

                case "Products":
                    _mainFrame.Navigate(new ProductsPage());
                    IsSalesSelected = false;
                    IsProductsSelected = true;
                    break;

                case "Universal":
                    _mainFrame.Navigate(new UniversalEditPage());
                    IsSalesSelected = false;
                    IsProductsSelected = false;
                    break;

                case "Categories":
                    // _mainFrame.Navigate(new CategoriesPage());
                    break;

                case "Employees":
                    _mainFrame.Navigate(new EmployeesPage());
                    IsSalesSelected = false;
                    IsProductsSelected = false;
                    break;

                case "Reports":
                    _mainFrame.Navigate(new ReportsPage());
                    IsSalesSelected = false;
                    IsProductsSelected = false;
                    break;

                case "Torg12":
                    _mainFrame.Navigate(new Torg12Page());
                    IsSalesSelected = false;
                    IsProductsSelected = false;
                    break;

                case "History":
                    _mainFrame.Navigate(new HistoryPage());
                    IsSalesSelected = false;
                    IsProductsSelected = false;
                    break;

                case "ViewSettings":
                    _mainFrame.Navigate(new ViewSettingsPage());
                    IsSalesSelected = false;
                    IsProductsSelected = false;
                    break;
                case "ViewDisplay":
                    _mainFrame.Navigate(new ViewDisplayPage());
                    IsSalesSelected = false;
                    IsProductsSelected = false;
                    break;

            }
        }

        private void Logout(object parameter)
        {
            var result = MessageBox.Show("Вы действительно хотите выйти?",
                                        "Подтверждение",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                App.CurrentUser = null;

                var authWindow = new AuthorizationWindow();
                authWindow.Show();

                foreach (Window window in Application.Current.Windows)
                {
                    if (window is MainWindow)
                    {
                        window.Close();
                        break;
                    }
                }
            }
        }
    }
}