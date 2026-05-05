using DiplomMurtazin.Core;
using DiplomMurtazin.Model;
using DiplomMurtazin.View;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace DiplomMurtazin.ViewModel
{
    public class SalesViewModel : BaseViewModel
    {
        private KPMurtazinEntities _context;
        private ObservableCollection<Products> _products;
        private ObservableCollection<SaleItemViewModel> _saleItems;
        private Products _selectedProduct;
        private SaleItemViewModel _selectedSaleItem;
        private decimal _totalAmount;
        private string _statusMessage;
        private string _statusColor;
        private string _stockError;
        private bool _hasStockErrors;
        private ObservableCollection<Products> _allProducts;
        private ObservableCollection<Products> _filteredProducts;
        private ObservableCollection<Categories> _categories;
        private Categories _selectedCategoryFilter;
        private string _searchText;

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

        public Categories SelectedCategoryFilter
        {
            get => _selectedCategoryFilter;
            set
            {
                if (Set(ref _selectedCategoryFilter, value))
                    ApplyFilters();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (Set(ref _searchText, value))
                    ApplyFilters();
            }
        }

        public ICommand ResetFiltersCommand { get; }

        public ObservableCollection<Products> Products
        {
            get => _products;
            set => Set(ref _products, value);
        }

        public ObservableCollection<SaleItemViewModel> SaleItems
        {
            get => _saleItems;
            set => Set(ref _saleItems, value);
        }

        public Products SelectedProduct
        {
            get => _selectedProduct;
            set => Set(ref _selectedProduct, value);
        }

        public SaleItemViewModel SelectedSaleItem
        {
            get => _selectedSaleItem;
            set => Set(ref _selectedSaleItem, value);
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set => Set(ref _totalAmount, value);
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

        public string StockError
        {
            get => _stockError;
            set => Set(ref _stockError, value);
        }

        public bool HasStockErrors
        {
            get => _hasStockErrors;
            set => Set(ref _hasStockErrors, value);
        }

        public ICommand LoadedCommand { get; }
        public ICommand AddToSaleCommand { get; }
        public ICommand RemoveFromSaleCommand { get; }
        public ICommand ClearSaleCommand { get; }
        public ICommand CompleteSaleCommand { get; }

        public SalesViewModel()
        {
            LoadedCommand = new RelayCommand(OnLoaded);
            AddToSaleCommand = new RelayCommand(AddToSale, CanAddToSale);
            RemoveFromSaleCommand = new RelayCommand(RemoveFromSale, CanRemoveFromSale);
            ClearSaleCommand = new RelayCommand(ClearSale);
            CompleteSaleCommand = new RelayCommand(CompleteSale, CanCompleteSale);

            ResetFiltersCommand = new RelayCommand(ResetFilters);

            SaleItems = new ObservableCollection<SaleItemViewModel>();
        }

        private void OnLoaded(object parameter)
        {
            LoadData();
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
            if (_allProducts == null) return;

            try
            {
                var query = _allProducts.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    string search = SearchText.ToLower();
                    query = query.Where(p =>
                        p.ProductName.ToLower().Contains(search) ||
                        (p.Manufacturer != null && p.Manufacturer.ToLower().Contains(search)) ||
                        (p.Barcode != null && p.Barcode.Contains(SearchText))
                    );
                }

                // Фильтр по категории (если выбрана не "Все категории")
                if (SelectedCategoryFilter != null && SelectedCategoryFilter.CategoryID > 0)
                {
                    query = query.Where(p => p.CategoryID == SelectedCategoryFilter.CategoryID);
                }

                FilteredProducts = new ObservableCollection<Products>(query.OrderBy(p => p.ProductName));
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
        private void LoadData()
        {
            try
            {
                _context = new KPMurtazinEntities();

                var productsList = _context.Products
                    .Include("Categories")
                    .ToList();

                // Загружаем остатки
                foreach (var product in productsList)
                {
                    var stock = _context.StockBalances
                        .Where(sb => sb.ProductID == product.ProductID)
                        .Sum(sb => (int?)sb.Quantity);

                    product.StockQuantity = stock.GetValueOrDefault(0);
                }

                Products = new ObservableCollection<Products>(productsList);
                _allProducts = new ObservableCollection<Products>(Products);
                FilteredProducts = new ObservableCollection<Products>(Products);
                LoadCategories(); // Вызвать после загрузки категорий
                SetStatus("Готов к работе", false);
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка загрузки: {ex.Message}", true);
            }
        }

        private bool CanAddToSale(object parameter)
        {
            return SelectedProduct != null && SelectedProduct.StockQuantity > 0;
        }

        private void AddToSale(object parameter)
        {
            try
            {
                // Проверяем, есть ли уже такой товар в чеке
                var existingItem = SaleItems.FirstOrDefault(x => x.ProductID == SelectedProduct.ProductID);

                if (existingItem != null)
                {
                    existingItem.Quantity++;
                }
                else
                {
                    SaleItems.Add(new SaleItemViewModel
                    {
                        ProductID = SelectedProduct.ProductID,
                        ProductName = SelectedProduct.ProductName,
                        UnitPrice = SelectedProduct.UnitPrice,
                        Quantity = 1
                    });
                }

                RecalculateTotal();
                CheckStockAvailability();
                SetStatus($"Товар '{SelectedProduct.ProductName}' добавлен в чек", false);
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка: {ex.Message}", true);
            }
        }

        private bool CanRemoveFromSale(object parameter)
        {
            return SelectedSaleItem != null;
        }

        private void RemoveFromSale(object parameter)
        {
            SaleItems.Remove(SelectedSaleItem);
            RecalculateTotal();
            CheckStockAvailability();
            SetStatus("Товар удален из чека", false);
        }

        private void ClearSale(object parameter)
        {
            SaleItems.Clear();
            RecalculateTotal();
            CheckStockAvailability();
            SetStatus("Чек очищен", false);
        }

        public bool CheckStockAvailability()
        {
            HasStockErrors = false;
            StockError = "";

            foreach (var item in SaleItems)
            {
                var product = Products.FirstOrDefault(p => p.ProductID == item.ProductID);
                if (product != null && product.StockQuantity < item.Quantity)
                {
                    HasStockErrors = true;
                    StockError += $" Недостаточно '{product.ProductName}' на складе. Доступно: {product.StockQuantity}\n";
                }
            }

            return !HasStockErrors;
        }

        private bool CanCompleteSale(object parameter)
        {
            return SaleItems.Count > 0 && !HasStockErrors;
        }

        private void CompleteSale(object parameter)
        {
            try
            {
                // Проверка остатков перед продажей
                if (!CheckStockAvailability())
                {
                    return;
                }

                // Создание продажи
                var sale = new Sales
                {
                    SaleDateTime = DateTime.Now,
                    EmployeeID = App.CurrentUser?.EmployeeID ?? 1,
                    ShiftID = 1,
                    TotalAmount = TotalAmount,
                    PaymentMethod = "Карта",
                    CashRegisterID = "CAS-001",
                    FiscalDocumentNumber = $"ФД-{DateTime.Now:yyyyMMddHHmmss}"
                };

                _context.Sales.Add(sale);
                _context.SaveChanges();

                // Добавление позиций
                foreach (var item in SaleItems)
                {
                    var saleItem = new SaleItems
                    {
                        SaleID = sale.SaleID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        BarcodeScanned = Products.First(p => p.ProductID == item.ProductID).Barcode
                    };
                    _context.SaleItems.Add(saleItem);
                    if (!InputMask.IsValidQuantity(item.Quantity))
                    {
                        SetStatus("Количество должно быть больше 0", true);
                        return;
                    }
                    // Обновление остатка
                    var stock = _context.StockBalances
                        .FirstOrDefault(sb => sb.ProductID == item.ProductID);

                    if (stock != null)
                    {
                        stock.Quantity -= item.Quantity;
                        stock.LastUpdated = DateTime.Now;
                    }
                }

                _context.SaveChanges();

                // Создание чека
                var receipt = CreateReceipt(sale);

                // Открываем окно чека
                var receiptWindow = new ReceiptWindow(receipt);
                receiptWindow.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is MainWindow);
                receiptWindow.ShowDialog();

                int soldItemsCount = SaleItems.Count;
                // Очистка чека и обновление данных
                ClearSale(null);
                LoadData();
                AuditLogger.Log("CREATE", "Sale", $"Оформлена продажа №{sale.SaleID} на сумму {sale.TotalAmount:F2} ₽", sale.SaleID.ToString(), $"ItemsCount={soldItemsCount}");

                SetStatus($"Продажа оформлена! Сумма: {TotalAmount:F2} ₽", false);
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка оформления продажи: {ex.Message}", true);
            }
        }

        private ReceiptModel CreateReceipt(Sales sale)
        {
            var random = new Random();

            var receipt = new ReceiptModel
            {
                SaleNumber = sale.SaleID,
                ShiftNumber = sale.ShiftID,
                Cashier = App.CurrentUser?.Login ?? "АДМИНИСТРАТОР",
                DateTime = DateTime.Now,
                TotalAmount = sale.TotalAmount,
                AmountWithoutVat = sale.TotalAmount, // В упрощенном варианте
                CashPayment = sale.TotalAmount,
                FdNumber = random.Next(100000, 999999),
                Fp = random.Next(100000000, 999999999).ToString(),
                DocumentNumber = random.Next(1, 9999)
            };

            foreach (var item in SaleItems)
            {
                receipt.Items.Add(new ReceiptItem
                {
                    Name = item.ProductName.ToUpper(),
                    Price = item.UnitPrice,
                    Quantity = item.Quantity
                });
            }

            return receipt;
        }

        private void RecalculateTotal()
        {
            TotalAmount = SaleItems.Sum(x => x.TotalPrice);
        }

        private void SetStatus(string message, bool isError)
        {
            StatusMessage = message;
            StatusColor = isError ? "#e74c3c" : "#3498db";
        }
    }

    // Вспомогательный класс для позиций в чеке
    public class SaleItemViewModel : BaseViewModel
    {
        private int _productId;
        private string _productName;
        private int _quantity;
        private decimal _unitPrice;

        public int ProductID
        {
            get => _productId;
            set => Set(ref _productId, value);
        }

        public string ProductName
        {
            get => _productName;
            set => Set(ref _productName, value);
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (Set(ref _quantity, value))
                {
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (Set(ref _unitPrice, value))
                {
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        public decimal TotalPrice => Quantity * UnitPrice;
    }
}