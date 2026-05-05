using DiplomMurtazin.Core;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace DiplomMurtazin.ViewModel
{
    public class MissingImportItem : BaseViewModel
    {
        public int MissingID { get; set; }
        public int Torg12ID { get; set; }
        public string TempProductName { get; set; }
        public string TempBarcode { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Status { get; set; }
    }

    public class Torg12LineItem : BaseViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string Barcode { get; set; }
        public int AvailableStock { get; set; }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (Set(ref _quantity, value))
                {
                    OnPropertyChanged(nameof(Total));
                }
            }
        }

        public decimal UnitPrice { get; set; }
        public decimal Total => UnitPrice * Quantity;
    }

    public class Torg12ViewModel : BaseViewModel
    {
        private ObservableCollection<Products> _allProducts = new ObservableCollection<Products>();
        private ObservableCollection<Products> _filteredProducts = new ObservableCollection<Products>();
        private Products _selectedProduct;
        private string _searchText;

        private ObservableCollection<Torg12LineItem> _items = new ObservableCollection<Torg12LineItem>();
        private Torg12LineItem _selectedItem;

        private string _documentNumber;
        private DateTime _documentDate = DateTime.Today;
        private string _receiverName;
        private string _receiverAddress;
        private string _basis;

        private string _statusMessage = "Готово";
        private string _statusColor = "#3498db";

        private ObservableCollection<MissingImportItem> _missingItems = new ObservableCollection<MissingImportItem>();
        private MissingImportItem _selectedMissingItem;

        public ObservableCollection<MissingImportItem> MissingItems
        {
            get => _missingItems;
            set => Set(ref _missingItems, value);
        }

        public MissingImportItem SelectedMissingItem
        {
            get => _selectedMissingItem;
            set => Set(ref _selectedMissingItem, value);
        }

        public ObservableCollection<Products> FilteredProducts
        {
            get => _filteredProducts;
            set => Set(ref _filteredProducts, value);
        }

        public Products SelectedProduct
        {
            get => _selectedProduct;
            set => Set(ref _selectedProduct, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (Set(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }

        public ObservableCollection<Torg12LineItem> Items
        {
            get => _items;
            set => Set(ref _items, value);
        }

        public Torg12LineItem SelectedItem
        {
            get => _selectedItem;
            set => Set(ref _selectedItem, value);
        }

        public string DocumentNumber
        {
            get => _documentNumber;
            set => Set(ref _documentNumber, value);
        }

        public DateTime DocumentDate
        {
            get => _documentDate;
            set => Set(ref _documentDate, value);
        }

        public string ReceiverName
        {
            get => _receiverName;
            set => Set(ref _receiverName, value);
        }

        public string ReceiverAddress
        {
            get => _receiverAddress;
            set => Set(ref _receiverAddress, value);
        }

        public string Basis
        {
            get => _basis;
            set => Set(ref _basis, value);
        }

        public decimal TotalAmount => Items.Sum(i => i.Total);

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

        public ICommand LoadedCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SaveDraftCommand { get; }
        public ICommand ExportTorg12Command { get; }
        public ICommand ImportTorg12Command { get; }
        public ICommand CreateProductFromMissingCommand { get; }

        public Torg12ViewModel()
        {
            LoadedCommand = new RelayCommand(_ => LoadProducts());
            AddItemCommand = new RelayCommand(_ => AddSelectedProduct(), _ => SelectedProduct != null);
            RemoveItemCommand = new RelayCommand(_ => RemoveSelectedItem(), _ => SelectedItem != null);
            ClearCommand = new RelayCommand(_ => Clear());
            SaveDraftCommand = new RelayCommand(_ => SaveDraft(), _ => Items.Any());
            ExportTorg12Command = new RelayCommand(_ => ExportTorg12(), _ => Items.Any());
            ImportTorg12Command = new RelayCommand(_ => ImportTorg12());
            CreateProductFromMissingCommand = new RelayCommand(_ => CreateProductFromMissing(), _ => SelectedMissingItem != null);

            DocumentNumber = $"ТОРГ12-{DateTime.Now:yyyyMMddHHmmss}";
        }

        private void LoadProducts()
        {
            try
            {
                using (var context = new KPMurtazinEntities())
                {
                    var products = context.Products.OrderBy(p => p.ProductName).ToList();
                    _allProducts = new ObservableCollection<Products>(products);
                    FilteredProducts = new ObservableCollection<Products>(products);
                }
                LoadMissingItems();
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка загрузки товаров: {ex.Message}", true);
            }
        }

        private void LoadMissingItems()
        {
            try
            {
                using (var context = new KPMurtazinEntities())
                {
                    const string sql = @"
SELECT MissingID, Torg12ID, TempProductName, TempBarcode, Quantity, UnitPrice, Status
FROM dbo.Torg12ImportMissingItems
WHERE Status = N'Pending'
ORDER BY CreatedDate ASC";
                    var list = context.Database.SqlQuery<MissingImportItem>(sql).ToList();
                    MissingItems = new ObservableCollection<MissingImportItem>(list);
                }
            }
            catch
            {
                // ignore
            }
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredProducts = new ObservableCollection<Products>(_allProducts);
                return;
            }

            string search = SearchText.ToLower();
            var filtered = _allProducts.Where(p =>
                (p.ProductName ?? "").ToLower().Contains(search) ||
                (p.Barcode ?? "").Contains(SearchText)).ToList();

            FilteredProducts = new ObservableCollection<Products>(filtered);
        }

        private int GetAvailableStock(int productId)
        {
            try
            {
                using (var context = new KPMurtazinEntities())
                {
                    return context.StockBalances.Where(sb => sb.ProductID == productId).Sum(sb => (int?)sb.Quantity) ?? 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        private void AddSelectedProduct()
        {
            if (SelectedProduct == null) return;

            int stock = GetAvailableStock(SelectedProduct.ProductID);
            var existing = Items.FirstOrDefault(i => i.ProductID == SelectedProduct.ProductID);
            if (existing != null)
            {
                if (existing.Quantity + 1 > stock)
                {
                    SetStatus($"Недостаточно '{existing.ProductName}' на складе (доступно {stock})", true);
                    return;
                }

                existing.Quantity += 1;
                OnPropertyChanged(nameof(TotalAmount));
                return;
            }

            if (stock <= 0)
            {
                SetStatus($"Нет остатка для '{SelectedProduct.ProductName}'", true);
                return;
            }

            Items.Add(new Torg12LineItem
            {
                ProductID = SelectedProduct.ProductID,
                ProductName = SelectedProduct.ProductName,
                Barcode = SelectedProduct.Barcode,
                AvailableStock = stock,
                Quantity = 1,
                UnitPrice = SelectedProduct.UnitPrice
            });

            OnPropertyChanged(nameof(TotalAmount));
            SetStatus($"Товар добавлен: {SelectedProduct.ProductName}", false);
        }

        private void RemoveSelectedItem()
        {
            if (SelectedItem == null) return;
            Items.Remove(SelectedItem);
            OnPropertyChanged(nameof(TotalAmount));
            SetStatus("Позиция удалена", false);
        }

        private void Clear()
        {
            Items.Clear();
            ReceiverName = "";
            ReceiverAddress = "";
            Basis = "";
            DocumentNumber = $"ТОРГ12-{DateTime.Now:yyyyMMddHHmmss}";
            DocumentDate = DateTime.Today;
            OnPropertyChanged(nameof(TotalAmount));
            SetStatus("Форма очищена", false);
        }

        private void SaveDraft()
        {
            try
            {
                // Получатель может быть любым текстом (не связан с БД). Пустое значение тоже допустимо.
                if (ReceiverName == null) ReceiverName = "";

                using (var context = new KPMurtazinEntities())
                {
                    // Raw SQL to avoid EDMX regeneration.
                    const string insertDoc = @"
INSERT INTO dbo.Torg12Documents (DocumentNumber, DocumentDate, ReceiverName, ReceiverAddress, Basis, CreatedByUserID, CreatedByEmployeeID, Status, Notes)
VALUES (@num, @date, @recv, @addr, @basis, @uid, @eid, N'Draft', NULL);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int? uid = App.CurrentUser?.UserID;
                    int? eid = App.CurrentUser?.EmployeeID;

                    int torgId = context.Database.SqlQuery<int>(
                        insertDoc,
                        new System.Data.SqlClient.SqlParameter("@num", DocumentNumber),
                        new System.Data.SqlClient.SqlParameter("@date", DocumentDate),
                        new System.Data.SqlClient.SqlParameter("@recv", string.IsNullOrWhiteSpace(ReceiverName) ? "Получатель" : ReceiverName),
                        new System.Data.SqlClient.SqlParameter("@addr", (object)ReceiverAddress ?? DBNull.Value),
                        new System.Data.SqlClient.SqlParameter("@basis", (object)Basis ?? DBNull.Value),
                        new System.Data.SqlClient.SqlParameter("@uid", (object)uid ?? DBNull.Value),
                        new System.Data.SqlClient.SqlParameter("@eid", (object)eid ?? DBNull.Value)
                    ).First();

                    foreach (var item in Items)
                    {
                        const string insertItem = @"
INSERT INTO dbo.Torg12Items (Torg12ID, ProductID, Quantity, UnitPrice)
VALUES (@tid, @pid, @qty, @price);";
                        context.Database.ExecuteSqlCommand(
                            insertItem,
                            new System.Data.SqlClient.SqlParameter("@tid", torgId),
                            new System.Data.SqlClient.SqlParameter("@pid", item.ProductID),
                            new System.Data.SqlClient.SqlParameter("@qty", item.Quantity),
                            new System.Data.SqlClient.SqlParameter("@price", item.UnitPrice)
                        );
                    }

                    AuditLogger.Log("CREATE", "TORG12", $"Создан черновик ТОРГ-12 №{DocumentNumber}", torgId.ToString(), $"Items={Items.Count}");
                }

                SetStatus("Черновик сохранен в базе", false);
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка сохранения ТОРГ-12: {ex.Message}", true);
            }
        }

        private void ExportTorg12()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel files (*.xls)|*.xls",
                FileName = $"ТОРГ12_{DocumentNumber}_{DateTime.Now:yyyyMMddHHmmss}.xls",
                DefaultExt = ".xls"
            };

            if (dialog.ShowDialog() != true)
            {
                SetStatus("Экспорт отменен", false);
                return;
            }

            try
            {
                string template = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SQL", "blanktorg12.xls");
                if (!File.Exists(template))
                {
                    // fallback to project SQL folder if running from VS
                    template = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\SQL\\blanktorg12.xls");
                }

                var rows = Items.Select(i => new Torg12ExcelRow
                {
                    ProductName = i.ProductName,
                    Barcode = i.Barcode,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList();

                Torg12ExcelInteropService.ExportFromTemplate(
                    template,
                    dialog.FileName,
                    DocumentNumber,
                    DocumentDate,
                    ReceiverName,
                    ReceiverAddress,
                    Basis,
                    rows
                );

                AuditLogger.Log("EXPORT", "TORG12", $"Экспорт ТОРГ-12 №{DocumentNumber}", metadata: $"File={Path.GetFileName(dialog.FileName)}");
                SetStatus($"ТОРГ-12 экспортирован: {Path.GetFileName(dialog.FileName)}", false);
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка экспорта: {ex.Message}", true);
                Console.WriteLine(ex.ToString());
            }
        }

        private void ImportTorg12()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Excel files (*.xls)|*.xls",
                Title = "Импорт ТОРГ-12"
            };

            if (dialog.ShowDialog() != true)
            {
                SetStatus("Импорт отменен", false);
                return;
            }

            try
            {
                var data = Torg12ExcelInteropService.Import(dialog.FileName);

                // Create draft doc in DB immediately (so it survives crashes)
                int torgId;
                using (var context = new KPMurtazinEntities())
                {
                    const string insertDoc = @"
INSERT INTO dbo.Torg12Documents (DocumentNumber, DocumentDate, ReceiverName, ReceiverAddress, Basis, CreatedByUserID, CreatedByEmployeeID, Status, Notes)
VALUES (@num, @date, @recv, @addr, @basis, @uid, @eid, N'Draft', N'Импорт из Excel');
SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    int? uid = App.CurrentUser?.UserID;
                    int? eid = App.CurrentUser?.EmployeeID;

                    torgId = context.Database.SqlQuery<int>(
                        insertDoc,
                        new System.Data.SqlClient.SqlParameter("@num", (object)data.DocumentNumber ?? DocumentNumber),
                        new System.Data.SqlClient.SqlParameter("@date", data.DocumentDate),
                        new System.Data.SqlClient.SqlParameter("@recv", (object)data.ReceiverName ?? "Получатель"),
                        new System.Data.SqlClient.SqlParameter("@addr", (object)data.ReceiverAddress ?? DBNull.Value),
                        new System.Data.SqlClient.SqlParameter("@basis", (object)data.Basis ?? DBNull.Value),
                        new System.Data.SqlClient.SqlParameter("@uid", (object)uid ?? DBNull.Value),
                        new System.Data.SqlClient.SqlParameter("@eid", (object)eid ?? DBNull.Value)
                    ).First();

                    // First add existing products
                    foreach (var row in data.Rows)
                    {
                        var barcode = (row.Barcode ?? "").Trim();
                        var name = (row.ProductName ?? "").Trim();

                        var product = !string.IsNullOrWhiteSpace(barcode)
                            ? context.Products.FirstOrDefault(p => p.Barcode == barcode)
                            : null;

                        if (product == null && !string.IsNullOrWhiteSpace(name))
                        {
                            product = context.Products.FirstOrDefault(p => p.ProductName == name);
                        }

                        if (product != null)
                        {
                            const string insertItem = @"
INSERT INTO dbo.Torg12Items (Torg12ID, ProductID, Quantity, UnitPrice)
VALUES (@tid, @pid, @qty, @price);";
                            context.Database.ExecuteSqlCommand(
                                insertItem,
                                new System.Data.SqlClient.SqlParameter("@tid", torgId),
                                new System.Data.SqlClient.SqlParameter("@pid", product.ProductID),
                                new System.Data.SqlClient.SqlParameter("@qty", row.Quantity),
                                new System.Data.SqlClient.SqlParameter("@price", row.UnitPrice <= 0 ? product.UnitPrice : row.UnitPrice)
                            );
                        }
                        else
                        {
                            const string insertMissing = @"
INSERT INTO dbo.Torg12ImportMissingItems (Torg12ID, TempProductName, TempBarcode, Quantity, UnitPrice, Status)
VALUES (@tid, @name, @barcode, @qty, @price, N'Pending');";
                            context.Database.ExecuteSqlCommand(
                                insertMissing,
                                new System.Data.SqlClient.SqlParameter("@tid", torgId),
                                new System.Data.SqlClient.SqlParameter("@name", name),
                                new System.Data.SqlClient.SqlParameter("@barcode", (object)barcode ?? DBNull.Value),
                                new System.Data.SqlClient.SqlParameter("@qty", row.Quantity),
                                new System.Data.SqlClient.SqlParameter("@price", row.UnitPrice)
                            );
                        }
                    }
                }

                // Load into current UI from DB (existing items only)
                DocumentNumber = data.DocumentNumber ?? DocumentNumber;
                DocumentDate = data.DocumentDate;
                ReceiverName = data.ReceiverName;
                ReceiverAddress = data.ReceiverAddress;
                Basis = data.Basis;

                Items.Clear();
                using (var context = new KPMurtazinEntities())
                {
                    var items = context.Database.SqlQuery<dynamic>(@"
SELECT ti.ProductID, p.ProductName, p.Barcode, ti.Quantity, ti.UnitPrice
FROM dbo.Torg12Items ti
JOIN dbo.Products p ON p.ProductID = ti.ProductID
WHERE ti.Torg12ID = @tid",
                        new System.Data.SqlClient.SqlParameter("@tid", torgId)).ToList();

                    foreach (dynamic it in items)
                    {
                        int pid = (int)it.ProductID;
                        Items.Add(new Torg12LineItem
                        {
                            ProductID = pid,
                            ProductName = (string)it.ProductName,
                            Barcode = (string)it.Barcode,
                            AvailableStock = GetAvailableStock(pid),
                            Quantity = (int)it.Quantity,
                            UnitPrice = (decimal)it.UnitPrice
                        });
                    }
                }

                LoadMissingItems();
                AuditLogger.Log("IMPORT", "TORG12", $"Импортирован ТОРГ-12 из Excel (черновик ID={torgId})", torgId.ToString(), $"Missing={MissingItems.Count}");
                SetStatus($"Импорт выполнен. Требуют создания товаров: {MissingItems.Count}", MissingItems.Count > 0);
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка импорта: {ex.Message}", true);
                Console.WriteLine(ex.ToString());
            }
        }

        private void CreateProductFromMissing()
        {
            if (SelectedMissingItem == null) return;

            try
            {
                // Open product editor prefilled
                var prefill = new Products
                {
                    ProductName = SelectedMissingItem.TempProductName ?? "",
                    Barcode = SelectedMissingItem.TempBarcode ?? "",
                    UnitPrice = SelectedMissingItem.UnitPrice,
                    WarrantyMonths = 12,
                    MinStockLevel = 5,
                    CategoryID = 1
                };

                var wnd = new View.ProductEditWindow(prefill);
                wnd.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is View.MainWindow);
                if (wnd.ShowDialog() == true)
                {
                    var created = wnd.GetProduct();
                    using (var context = new KPMurtazinEntities())
                    {
                        context.Products.Add(created);
                        context.SaveChanges();

                        // Link missing -> created product and add to Torg12Items
                        context.Database.ExecuteSqlCommand(@"
UPDATE dbo.Torg12ImportMissingItems
SET CreatedProductID=@pid, Status=N'Resolved'
WHERE MissingID=@mid;
INSERT INTO dbo.Torg12Items (Torg12ID, ProductID, Quantity, UnitPrice)
VALUES (@tid, @pid, @qty, @price);",
                            new System.Data.SqlClient.SqlParameter("@pid", created.ProductID),
                            new System.Data.SqlClient.SqlParameter("@mid", SelectedMissingItem.MissingID),
                            new System.Data.SqlClient.SqlParameter("@tid", SelectedMissingItem.Torg12ID),
                            new System.Data.SqlClient.SqlParameter("@qty", SelectedMissingItem.Quantity),
                            new System.Data.SqlClient.SqlParameter("@price", SelectedMissingItem.UnitPrice <= 0 ? created.UnitPrice : SelectedMissingItem.UnitPrice)
                        );
                    }

                    AuditLogger.Log("CREATE", "Product", $"Создан товар из импорта ТОРГ-12: '{created.ProductName}'", created.ProductID.ToString());
                    LoadProducts(); // refresh products + missing
                    SetStatus("Товар создан и добавлен в ТОРГ-12", false);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Ошибка создания товара: {ex.Message}", true);
            }
        }

        private void SetStatus(string message, bool isError)
        {
            StatusMessage = message;
            StatusColor = isError ? "#e74c3c" : "#3498db";
        }
    }
}

