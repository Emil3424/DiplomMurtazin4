using DiplomMurtazin.Core;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace DiplomMurtazin.ViewModel
{
    public class ProductEditViewModel : BaseViewModel
    {
        private Products _product;
        private ObservableCollection<Categories> _categories;
        private Categories _selectedCategory;
        private string _title;
        private string _errorMessage;
        private bool _isNewProduct;

        public Products Product
        {
            get => _product;
            set => Set(ref _product, value);
        }

        public ObservableCollection<Categories> Categories
        {
            get => _categories;
            set => Set(ref _categories, value);
        }

        public Categories SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (Set(ref _selectedCategory, value) && value != null && Product != null)
                {
                    Product.CategoryID = value.CategoryID;
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

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public ProductEditViewModel(Products product = null)
        {
            // Сначала инициализируем Product
            if (product == null)
            {
                _isNewProduct = true;
                Title = "Добавление товара";
                Product = new Products
                {
                    ProductName = "",
                    Barcode = "",
                    UnitPrice = 0,
                    WarrantyMonths = 12,
                    MinStockLevel = 5,
                    CategoryID = 1 // Временное значение
                };
            }
            else
            {
                _isNewProduct = false;
                Title = "Редактирование товара";
                // Создаем копию, чтобы не изменять оригинал до сохранения
                Product = new Products
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    Barcode = product.Barcode,
                    CategoryID = product.CategoryID,
                    Manufacturer = product.Manufacturer,
                    Model = product.Model,
                    UnitPrice = product.UnitPrice,
                    WarrantyMonths = product.WarrantyMonths,
                    MinStockLevel = product.MinStockLevel,
                    Description = product.Description
                };
            }

            // Потом загружаем категории
            LoadCategories();

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void LoadCategories()
        {
            try
            {
                using (var db = new KPMurtazinEntities())
                {
                    var categoriesList = db.Categories.ToList();
                    Categories = new ObservableCollection<Categories>(categoriesList);

                    // Устанавливаем выбранную категорию после загрузки списка
                    if (Product != null && Product.CategoryID > 0)
                    {
                        SelectedCategory = Categories.FirstOrDefault(c => c.CategoryID == Product.CategoryID);
                    }

                    // Если категория не найдена, выбираем первую
                    if (SelectedCategory == null && Categories.Any())
                    {
                        SelectedCategory = Categories.First();
                        if (Product != null)
                        {
                            Product.CategoryID = SelectedCategory.CategoryID;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки категорий: {ex.Message}";
            }
        }

        private void Save(object param)
        {
            // Проверка на null
            if (Product == null)
            {
                ErrorMessage = "Ошибка инициализации товара";
                return;
            }

            if (string.IsNullOrWhiteSpace(Product.ProductName))
            {
                ErrorMessage = "Введите название товара";
                return;
            }

            if (string.IsNullOrWhiteSpace(Product.Barcode))
            {
                ErrorMessage = "Введите штрих-код";
                return;
            }

            if (Product.UnitPrice < 0)
            {
                ErrorMessage = "Цена не может быть отрицательной";
                return;
            }

            if (SelectedCategory == null)
            {
                ErrorMessage = "Выберите категорию";
                return;
            }

            // Обновляем CategoryID из выбранной категории
            Product.CategoryID = SelectedCategory.CategoryID;

            // Проверка уникальности штрих-кода для новых товаров
            if (_isNewProduct)
            {
                using (var db = new KPMurtazinEntities())
                {
                    if (db.Products.Any(p => p.Barcode == Product.Barcode))
                    {
                        ErrorMessage = "Товар с таким штрих-кодом уже существует";
                        return;
                    }
                }
            }

            // Закрываем окно с успехом
            var window = param as Window;
            if (window != null)
            {
                window.DialogResult = true;
                window.Close();
            }
        }

        private void Cancel(object param)
        {
            var window = param as Window;
            if (window != null)
            {
                window.DialogResult = false;
                window.Close();
            }
        }
    }
}