using DiplomMurtazin.Core;
using DiplomMurtazin.View;
using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace DiplomMurtazin.ViewModel
{
    public class ProductsViewModel : BaseViewModel
    {
        private KPMurtazinEntities _context;
        private ObservableCollection<Products> _allProducts;
        private ObservableCollection<Products> _filteredProducts;
        private ObservableCollection<Categories> _categories;
        private Products _selectedProduct;
        private Categories _selectedCategoryFilter;
        private string _searchText;
        private string _statusMessage;
        private string _statusColor;
        private int _totalCount;
        private int _filteredCount;

        public ObservableCollection<Products> FilteredProducts
        {
            get => _filteredProducts;
            set => Set(ref _filteredProducts, value);
        }

        public ObservableCollection<Categories> Categories
        {
            get => _categories;
            set => Set(ref _categories, value);
        }

        public Products SelectedProduct
        {
            get => _selectedProduct;
            set => Set(ref _selectedProduct, value);
        }

        public Categories SelectedCategoryFilter
        {
            get => _selectedCategoryFilter;
            set
            {
                if (Set(ref _selectedCategoryFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (Set(ref _searchText, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => Set(ref _statusMessage, value);
        }

        public string StatusColor
        {
            get => _statusColor;
            set => Set(ref _statusColor, value);
        }

        public string FilteredProductsCount => $"{_filteredCount} / {_totalCount}";

        public ICommand LoadedCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ResetFiltersCommand { get; }

        public ProductsViewModel()
        {
            LoadedCommand = new RelayCommand(OnLoaded);
            AddCommand = new RelayCommand(OpenAddWindow);
            EditCommand = new RelayCommand(OpenEditWindow, CanEditDelete);
            DeleteCommand = new RelayCommand(DeleteProduct, CanEditDelete);
            RefreshCommand = new RelayCommand(RefreshData);
            ResetFiltersCommand = new RelayCommand(ResetFilters);

            _allProducts = new ObservableCollection<Products>();
            _filteredProducts = new ObservableCollection<Products>();
            _categories = new ObservableCollection<Categories>();
        }

        private bool CanEditDelete(object parameter)
        {
            return SelectedProduct != null;
        }

        private void OnLoaded(object parameter)
        {
            LoadData();
            LoadCategories();
        }

        private void LoadData()
        {
            try
            {
                _context = new KPMurtazinEntities();

                // Загружаем все товары
                var productsList = _context.Products
                    .Include(p => p.Categories)
                    .OrderBy(p => p.ProductName)
                    .ToList();

                // Загружаем остатки
                foreach (var product in productsList)
                {
                    var stock = _context.StockBalances
                        .Where(sb => sb.ProductID == product.ProductID)
                        .Sum(sb => (int?)sb.Quantity);

                    product.StockQuantity = stock ?? 0;
                }

                _allProducts.Clear();
                foreach (var product in productsList)
                {
                    _allProducts.Add(product);
                }

                _totalCount = _allProducts.Count;
                ApplyFilters();

                SetStatus("Готов к работе", false);
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка загрузки: {ex.Message}", true);
            }
        }

        private void LoadCategories()
        {
            try
            {
                using (var context = new KPMurtazinEntities())
                {
                    var categoriesList = context.Categories.OrderBy(c => c.CategoryName).ToList();

                    // Создаем элемент "Все категории"
                    var allCategories = new Categories
                    {
                        CategoryID = 0,
                        CategoryName = "Все категории"
                    };

                    categoriesList.Insert(0, allCategories);

                    Categories = new ObservableCollection<Categories>(categoriesList);

                    // Устанавливаем "Все категории" как выбранное
                    SelectedCategoryFilter = allCategories;
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка загрузки категорий: {ex.Message}", true);
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var filtered = _allProducts.AsEnumerable();

                // Фильтр по поисковому тексту
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    string searchLower = SearchText.ToLower();
                    filtered = filtered.Where(p =>
                        p.ProductName.ToLower().Contains(searchLower) ||
                        (p.Manufacturer != null && p.Manufacturer.ToLower().Contains(searchLower)) ||
                        (p.Barcode != null && p.Barcode.Contains(SearchText))
                    );
                }

                // Фильтр по категории (если выбрана не "Все категории")
                if (SelectedCategoryFilter != null && SelectedCategoryFilter.CategoryID > 0)
                {
                    filtered = filtered.Where(p => p.CategoryID == SelectedCategoryFilter.CategoryID);
                }

                // Обновляем отфильтрованный список
                FilteredProducts.Clear();
                foreach (var product in filtered.OrderBy(p => p.ProductName))
                {
                    FilteredProducts.Add(product);
                }

                _filteredCount = FilteredProducts.Count;
                OnPropertyChanged(nameof(FilteredProductsCount));
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка фильтрации: {ex.Message}", true);
            }
        }

        private void ResetFilters(object parameter)
        {
            SearchText = "";

            // Сбрасываем на "Все категории"
            if (Categories != null)
            {
                SelectedCategoryFilter = Categories.FirstOrDefault(c => c.CategoryID == 0);
            }

            ApplyFilters();
            SetStatus("Фильтры сброшены", false);
        }

        private void OpenAddWindow(object parameter)
        {
            try
            {
                var editWindow = new ProductEditWindow();
                editWindow.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is MainWindow);

                if (editWindow.ShowDialog() == true)
                {
                    var newProduct = editWindow.GetProduct();

                    using (var context = new KPMurtazinEntities())
                    {
                        context.Products.Add(newProduct);
                        context.SaveChanges();
                        AuditLogger.Log("CREATE", "Product", $"Добавлен товар '{newProduct.ProductName}'", newProduct.ProductID.ToString());

                        RefreshData(null);
                        SetStatus("Товар успешно добавлен", false);
                    }
                }
                else
                {
                    SetStatus("Добавление товара отменено", false);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private void OpenEditWindow(object parameter)
        {
            if (SelectedProduct == null) return;

            try
            {
                var editWindow = new ProductEditWindow(SelectedProduct);
                editWindow.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is MainWindow);

                if (editWindow.ShowDialog() == true)
                {
                    var editedProduct = editWindow.GetProduct();

                    using (var context = new KPMurtazinEntities())
                    {
                        var dbProduct = context.Products.Find(editedProduct.ProductID);
                        if (dbProduct != null)
                        {
                            dbProduct.ProductName = editedProduct.ProductName;
                            dbProduct.Barcode = editedProduct.Barcode;
                            dbProduct.CategoryID = editedProduct.CategoryID;
                            dbProduct.Manufacturer = editedProduct.Manufacturer;
                            dbProduct.Model = editedProduct.Model;
                            dbProduct.UnitPrice = editedProduct.UnitPrice;
                            dbProduct.WarrantyMonths = editedProduct.WarrantyMonths;
                            dbProduct.MinStockLevel = editedProduct.MinStockLevel;
                            dbProduct.Description = editedProduct.Description;
                            dbProduct.PhotoData = editedProduct.PhotoData;
                            dbProduct.PhotoPath = editedProduct.PhotoPath;

                            context.SaveChanges();
                            AuditLogger.Log("UPDATE", "Product", $"Обновлен товар '{dbProduct.ProductName}'", dbProduct.ProductID.ToString());

                            RefreshData(null);
                            SetStatus("Товар успешно обновлен", false);
                        }
                    }
                }
                else
                {
                    SetStatus("Редактирование товара отменено", false);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private void DeleteProduct(object parameter)
        {
            if (SelectedProduct == null) return;

            var result = MessageBox.Show(
                $"Удалить товар '{SelectedProduct.ProductName}'?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new KPMurtazinEntities())
                    {
                        bool isUsed = context.SaleItems.Any(si => si.ProductID == SelectedProduct.ProductID) ||
                                      context.InventoryDetails.Any(id => id.ProductID == SelectedProduct.ProductID) ||
                                      context.InvoiceItems.Any(ii => ii.ProductID == SelectedProduct.ProductID) ||
                                      context.StockBalances.Any(sb => sb.ProductID == SelectedProduct.ProductID);

                        if (isUsed)
                        {
                            SetStatus("Нельзя удалить: товар используется в документах", true);
                            return;
                        }

                        var productToDelete = context.Products.Find(SelectedProduct.ProductID);
                        if (productToDelete != null)
                        {
                            context.Products.Remove(productToDelete);
                            context.SaveChanges();
                            AuditLogger.Log("DELETE", "Product", $"Удален товар '{productToDelete.ProductName}'", productToDelete.ProductID.ToString());

                            RefreshData(null);
                            SetStatus("Товар удален", false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    SetStatus($"Ошибка удаления: {ex.Message}", true);
                }
            }
            else
            {
                SetStatus("Удаление товара отменено", false);
            }
        }

        private void RefreshData(object parameter)
        {
            _context?.Dispose();
            LoadData();
            LoadCategories();
            SetStatus("Данные обновлены", false);
        }

        private void SetStatus(string message, bool isError)
        {
            StatusMessage = message;
            StatusColor = isError ? "#e74c3c" : "#3498db";
        }

        public bool HasChanges()
        {
            return false; // Упрощенно
        }

        public void DisposeContext()
        {
            _context?.Dispose();
        }
    }
}