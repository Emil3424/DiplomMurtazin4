using DiplomMurtazin.Core;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DiplomMurtazin.ViewModel
{
    public class EmployeeEditViewModel : BaseViewModel
    {
        private Employees _employee;
        private ObservableCollection<Positions> _positions;
        private Positions _selectedPosition;
        private string _title;
        private string _errorMessage;
        private bool _isNewEmployee;
        private BitmapImage _photoPreview;
        private string _tempPhotoPath;
        private readonly bool _isEditMode;

        public Employees Employee
        {
            get => _employee;
            set => Set(ref _employee, value);
        }

        public ObservableCollection<Positions> Positions
        {
            get => _positions;
            set => Set(ref _positions, value);
        }

        public Positions SelectedPosition
        {
            get => _selectedPosition;
            set
            {
                if (Set(ref _selectedPosition, value) && value != null && Employee != null)
                {
                    Employee.PositionID = value.PositionID;
                }
            }
        }

        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => Set(ref _errorMessage, value);
        }

        public BitmapImage PhotoPreview
        {
            get => _photoPreview;
            set => Set(ref _photoPreview, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SelectPhotoCommand { get; }
        public ICommand ClearPhotoCommand { get; }

        public EmployeeEditViewModel(Employees employee = null)
        {
            LoadPositions();

            if (employee == null)
            {
                Title = "Добавление сотрудника";
                Employee = new Employees
                {
                    HireDate = DateTime.Today,
                    IsActive = true
                };
            }
            else
            {
                _isNewEmployee = false;
                Title = "Редактирование сотрудника";

                // Создаем копию сотрудника
                Employee = new Employees
                {
                    EmployeeID = employee.EmployeeID,
                    LastName = employee.LastName,
                    FirstName = employee.FirstName,
                    MiddleName = employee.MiddleName,
                    PositionID = employee.PositionID,
                    HireDate = employee.HireDate,
                    Phone = employee.Phone,
                    Email = employee.Email,
                    Address = employee.Address,
                    PassportData = employee.PassportData,
                    INN = employee.INN,
                    IsActive = employee.IsActive,
                    PhotoData = employee.PhotoData,
                    PhotoPath = employee.PhotoPath
                };

                // Загружаем фото для предпросмотра
                if (Employee.PhotoData != null && Employee.PhotoData.Length > 0)
                {
                    PhotoPreview = LoadImageFromBytes(Employee.PhotoData);
                }
                else if (!string.IsNullOrEmpty(Employee.PhotoPath) && File.Exists(Employee.PhotoPath))
                {
                    try
                    {
                        PhotoPreview = LoadImageFromFile(Employee.PhotoPath);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Ошибка загрузки фото из файла: {ex.Message}");
                    }
                }
            }

            // Устанавливаем выбранную должность
            if (Employee.PositionID > 0 && Positions != null)
            {
                SelectedPosition = Positions.FirstOrDefault(p => p.PositionID == Employee.PositionID);
            }

            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            SelectPhotoCommand = new RelayCommand(SelectPhoto);
            ClearPhotoCommand = new RelayCommand(ClearPhoto);
        }

        private void LoadPositions()
        {
            try
            {
                using (var context = new KPMurtazinEntities())
                {
                    Positions = new ObservableCollection<Positions>(
                        context.Positions.OrderBy(p => p.PositionName).ToList()
                    );
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки должностей: {ex.Message}";
            }
        }

        private bool CanSave(object parameter)
        {
            return !string.IsNullOrWhiteSpace(Employee.LastName) &&
                   !string.IsNullOrWhiteSpace(Employee.FirstName) &&
                   Employee.PositionID > 0;
        }

        private void Save(object param)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(Employee.LastName))
            {
                ErrorMessage = "Введите фамилию";
                return;
            }

            if (string.IsNullOrWhiteSpace(Employee.FirstName))
            {
                ErrorMessage = "Введите имя";
                return;
            }

            if (Employee.PositionID == 0 && SelectedPosition != null)
            {
                Employee.PositionID = SelectedPosition.PositionID;
            }

            if (Employee.PositionID == 0)
            {
                ErrorMessage = "Выберите должность";
                return;
            }

            // Сохраняем фото в базу данных
            if (!string.IsNullOrEmpty(_tempPhotoPath) && File.Exists(_tempPhotoPath))
            {
                try
                {
                    Employee.PhotoData = File.ReadAllBytes(_tempPhotoPath);
                    Employee.PhotoPath = null; // Очищаем путь, используем данные
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Ошибка сохранения фото: {ex.Message}";
                    return;
                }
            }

            (param as Window).DialogResult = true;
        }

        private void Cancel(object param)
        {
            // Удаляем временное фото, если оно было
            if (!string.IsNullOrEmpty(_tempPhotoPath) && File.Exists(_tempPhotoPath))
            {
                try { File.Delete(_tempPhotoPath); } catch { }
            }

            (param as Window).DialogResult = false;
        }

        private void SelectPhoto(object parameter)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.jpeg, *.png) | *.jpg; *.jpeg; *.png",
                Title = "Выберите фотографию сотрудника"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Загружаем фото для предпросмотра
                    PhotoPreview = LoadImageFromFile(dialog.FileName);
                    _tempPhotoPath = dialog.FileName;
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Ошибка загрузки фото: {ex.Message}";
                }
            }
        }

        private void ClearPhoto(object parameter)
        {
            PhotoPreview = null;
            _tempPhotoPath = null;
            Employee.PhotoData = null;
            Employee.PhotoPath = null;
        }

        // Метод для загрузки изображения из файла
        private BitmapImage LoadImageFromFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(path, UriKind.Absolute);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.DecodePixelWidth = 300;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
                return null;
            }
        }

        // Метод для загрузки изображения из массива байт
        private BitmapImage LoadImageFromBytes(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
                return null;

            try
            {
                var image = new BitmapImage();

                using (var stream = new MemoryStream(imageData))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.DecodePixelWidth = 300;
                    image.EndInit();
                }

                image.Freeze();
                return image;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки изображения из байт: {ex.Message}");
                return null;
            }
        }
    }
}