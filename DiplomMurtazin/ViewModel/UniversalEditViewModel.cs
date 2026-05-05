using DiplomMurtazin.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows.Input;

namespace DiplomMurtazin.ViewModel
{
    public class TableInfo
    {
        public string Name { get; set; }
        public Type EntityType { get; set; }
        public Func<KPMurtazinEntities, IEnumerable> GetData { get; set; }
        public Action<KPMurtazinEntities, object> AddEntity { get; set; }
        public Action<KPMurtazinEntities, object> RemoveEntity { get; set; }
        public Func<object, object> GetId { get; set; }
    }

    public class UniversalEditViewModel : BaseViewModel
    {
        private KPMurtazinEntities _context;
        private ObservableCollection<TableInfo> _tables;
        private TableInfo _selectedTable;
        private List<object> _originalData;
        private DataTable _dataTable;
        private string _searchText;
        private string _statusMessage;
        private string _statusColor;

        public ObservableCollection<TableInfo> Tables
        {
            get => _tables;
            set => Set(ref _tables, value);
        }

        public TableInfo SelectedTable
        {
            get => _selectedTable;
            set
            {
                if (Set(ref _selectedTable, value))
                {
                    LoadTableData();
                }
            }
        }

        public DataTable DataTable
        {
            get => _dataTable;
            set => Set(ref _dataTable, value);
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
        public ICommand SaveCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ResetFiltersCommand { get; }

        public UniversalEditViewModel()
        {
            LoadedCommand = new RelayCommand(OnLoaded);
            SaveCommand = new RelayCommand(SaveChanges);
            RefreshCommand = new RelayCommand(RefreshData);
            ResetFiltersCommand = new RelayCommand(ResetFilters);

            InitializeTables();

            StatusColor = "#3498db";
            StatusMessage = "Готов к работе";
        }

        private void InitializeTables()
        {
            Tables = new ObservableCollection<TableInfo>
            {
                new TableInfo
                {
                    Name = "Категории",
                    EntityType = typeof(Categories),
                    GetData = (ctx) => ctx.Categories.OrderBy(c => c.CategoryName).ToList(),
                    AddEntity = (ctx, entity) => ctx.Categories.Add((Categories)entity),
                    RemoveEntity = (ctx, entity) => ctx.Categories.Remove((Categories)entity),
                    GetId = (obj) => ((Categories)obj).CategoryID
                },
                new TableInfo
                {
                    Name = "Должности",
                    EntityType = typeof(Positions),
                    GetData = (ctx) => ctx.Positions.OrderBy(p => p.PositionName).ToList(),
                    AddEntity = (ctx, entity) => ctx.Positions.Add((Positions)entity),
                    RemoveEntity = (ctx, entity) => ctx.Positions.Remove((Positions)entity),
                    GetId = (obj) => ((Positions)obj).PositionID
                },
                new TableInfo
                {
                    Name = "Поставщики",
                    EntityType = typeof(Suppliers),
                    GetData = (ctx) => ctx.Suppliers.OrderBy(s => s.SupplierName).ToList(),
                    AddEntity = (ctx, entity) => ctx.Suppliers.Add((Suppliers)entity),
                    RemoveEntity = (ctx, entity) => ctx.Suppliers.Remove((Suppliers)entity),
                    GetId = (obj) => ((Suppliers)obj).SupplierID
                },
                new TableInfo
                {
                    Name = "Складские зоны",
                    EntityType = typeof(WarehouseZones),
                    GetData = (ctx) => ctx.WarehouseZones.OrderBy(w => w.ZoneName).ToList(),
                    AddEntity = (ctx, entity) => ctx.WarehouseZones.Add((WarehouseZones)entity),
                    RemoveEntity = (ctx, entity) => ctx.WarehouseZones.Remove((WarehouseZones)entity),
                    GetId = (obj) => ((WarehouseZones)obj).ZoneID
                },
                new TableInfo
                {
                    Name = "Пользователи",
                    EntityType = typeof(Users),
                    GetData = (ctx) => ctx.Users.OrderBy(u => u.Login).ToList(),
                    AddEntity = (ctx, entity) => ctx.Users.Add((Users)entity),
                    RemoveEntity = (ctx, entity) => ctx.Users.Remove((Users)entity),
                    GetId = (obj) => ((Users)obj).UserID
                },
                new TableInfo
                {
                    Name = "Сотрудники",
                    EntityType = typeof(Employees),
                    GetData = (ctx) => ctx.Employees.OrderBy(e => e.LastName).ToList(),
                    AddEntity = (ctx, entity) => ctx.Employees.Add((Employees)entity),
                    RemoveEntity = (ctx, entity) => ctx.Employees.Remove((Employees)entity),
                    GetId = (obj) => ((Employees)obj).EmployeeID
                },
                new TableInfo
                {
                    Name = "Товары",
                    EntityType = typeof(Products),
                    GetData = (ctx) => ctx.Products.OrderBy(p => p.ProductName).ToList(),
                    AddEntity = (ctx, entity) => ctx.Products.Add((Products)entity),
                    RemoveEntity = (ctx, entity) => ctx.Products.Remove((Products)entity),
                    GetId = (obj) => ((Products)obj).ProductID
                },
                new TableInfo
                {
                    Name = "Продажи",
                    EntityType = typeof(Sales),
                    GetData = (ctx) => ctx.Sales.OrderByDescending(s => s.SaleDateTime).ToList(),
                    AddEntity = (ctx, entity) => ctx.Sales.Add((Sales)entity),
                    RemoveEntity = (ctx, entity) => ctx.Sales.Remove((Sales)entity),
                    GetId = (obj) => ((Sales)obj).SaleID
                },
                new TableInfo
                {
                    Name = "Позиции продаж",
                    EntityType = typeof(SaleItems),
                    GetData = (ctx) => ctx.SaleItems.OrderBy(s => s.SaleItemID).ToList(),
                    AddEntity = (ctx, entity) => ctx.SaleItems.Add((SaleItems)entity),
                    RemoveEntity = (ctx, entity) => ctx.SaleItems.Remove((SaleItems)entity),
                    GetId = (obj) => ((SaleItems)obj).SaleItemID
                },
                new TableInfo
                {
                    Name = "Акты инвентаризации",
                    EntityType = typeof(InventoryActs),
                    GetData = (ctx) => ctx.InventoryActs.OrderByDescending(i => i.InventoryDate).ToList(),
                    AddEntity = (ctx, entity) => ctx.InventoryActs.Add((InventoryActs)entity),
                    RemoveEntity = (ctx, entity) => ctx.InventoryActs.Remove((InventoryActs)entity),
                    GetId = (obj) => ((InventoryActs)obj).InventoryID
                },
                new TableInfo
                {
                    Name = "Детали инвентаризации",
                    EntityType = typeof(InventoryDetails),
                    GetData = (ctx) => ctx.InventoryDetails.OrderBy(i => i.InventoryDetailID).ToList(),
                    AddEntity = (ctx, entity) => ctx.InventoryDetails.Add((InventoryDetails)entity),
                    RemoveEntity = (ctx, entity) => ctx.InventoryDetails.Remove((InventoryDetails)entity),
                    GetId = (obj) => ((InventoryDetails)obj).InventoryDetailID
                },
                new TableInfo
                {
                    Name = "Накладные",
                    EntityType = typeof(Invoices),
                    GetData = (ctx) => ctx.Invoices.OrderByDescending(i => i.InvoiceDate).ToList(),
                    AddEntity = (ctx, entity) => ctx.Invoices.Add((Invoices)entity),
                    RemoveEntity = (ctx, entity) => ctx.Invoices.Remove((Invoices)entity),
                    GetId = (obj) => ((Invoices)obj).InvoiceID
                },
                new TableInfo
                {
                    Name = "Позиции накладных",
                    EntityType = typeof(InvoiceItems),
                    GetData = (ctx) => ctx.InvoiceItems.OrderBy(i => i.InvoiceItemID).ToList(),
                    AddEntity = (ctx, entity) => ctx.InvoiceItems.Add((InvoiceItems)entity),
                    RemoveEntity = (ctx, entity) => ctx.InvoiceItems.Remove((InvoiceItems)entity),
                    GetId = (obj) => ((InvoiceItems)obj).InvoiceItemID
                },
                new TableInfo
                {
                    Name = "Смены",
                    EntityType = typeof(Shifts),
                    GetData = (ctx) => ctx.Shifts.OrderByDescending(s => s.ShiftStart).ToList(),
                    AddEntity = (ctx, entity) => ctx.Shifts.Add((Shifts)entity),
                    RemoveEntity = (ctx, entity) => ctx.Shifts.Remove((Shifts)entity),
                    GetId = (obj) => ((Shifts)obj).ShiftID
                },
                new TableInfo
                {
                    Name = "Остатки",
                    EntityType = typeof(StockBalances),
                    GetData = (ctx) => ctx.StockBalances.OrderBy(s => s.StockID).ToList(),
                    AddEntity = (ctx, entity) => ctx.StockBalances.Add((StockBalances)entity),
                    RemoveEntity = (ctx, entity) => ctx.StockBalances.Remove((StockBalances)entity),
                    GetId = (obj) => ((StockBalances)obj).StockID
                },
                new TableInfo
                {
                    Name = "Зарплаты",
                    EntityType = typeof(Salaries),
                    GetData = (ctx) => ctx.Salaries.OrderByDescending(s => s.PeriodEnd).ToList(),
                    AddEntity = (ctx, entity) => ctx.Salaries.Add((Salaries)entity),
                    RemoveEntity = (ctx, entity) => ctx.Salaries.Remove((Salaries)entity),
                    GetId = (obj) => ((Salaries)obj).SalaryID
                },
                new TableInfo
                {
                    Name = "История движения",
                    EntityType = typeof(ProductMovementHistory),
                    GetData = (ctx) => ctx.ProductMovementHistory.OrderByDescending(m => m.MovementDate).ToList(),
                    AddEntity = (ctx, entity) => ctx.ProductMovementHistory.Add((ProductMovementHistory)entity),
                    RemoveEntity = (ctx, entity) => ctx.ProductMovementHistory.Remove((ProductMovementHistory)entity),
                    GetId = (obj) => ((ProductMovementHistory)obj).MovementID
                }
            };
        }

        private void OnLoaded(object parameter)
        {
            _context = new KPMurtazinEntities();
            _context.Configuration.ProxyCreationEnabled = false;
            _context.Configuration.LazyLoadingEnabled = false;

            // Устанавливаем поставщиков как выбранную таблицу по умолчанию
            SelectedTable = Tables.FirstOrDefault(t => t.Name == "Поставщики");
        }

        private void LoadTableData()
        {
            if (_selectedTable == null || _context == null) return;

            try
            {
                var data = _selectedTable.GetData(_context);
                _originalData = data.Cast<object>().ToList();

                CreateDataTable(_originalData);

                StatusMessage = $"Загружено записей: {_originalData.Count}";
                StatusColor = "#3498db";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private void CreateDataTable(List<object> data)
        {
            if (data == null || data.Count == 0)
            {
                DataTable = new DataTable();
                return;
            }

            var dt = new DataTable();
            var properties = data[0].GetType().GetProperties();

            foreach (var prop in properties)
            {
                if (prop.PropertyType.IsGenericType &&
                    (prop.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                     prop.PropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                    continue;

                if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string) &&
                    !prop.PropertyType.IsGenericType)
                    continue;

                Type columnType = prop.PropertyType;

                if (columnType.IsGenericType && columnType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    columnType = Nullable.GetUnderlyingType(columnType);
                }

                if (columnType.IsEnum)
                {
                    columnType = typeof(int);
                }

                dt.Columns.Add(prop.Name, columnType ?? typeof(string));
            }

            foreach (var item in data)
            {
                var row = dt.NewRow();
                foreach (DataColumn col in dt.Columns)
                {
                    var prop = properties.FirstOrDefault(p => p.Name == col.ColumnName);
                    if (prop != null)
                    {
                        var value = prop.GetValue(item);

                        if (value == null)
                        {
                            row[col] = DBNull.Value;
                        }
                        else if (Nullable.GetUnderlyingType(prop.PropertyType) != null)
                        {
                            var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
                            row[col] = Convert.ChangeType(value, underlyingType);
                        }
                        else if (prop.PropertyType.IsEnum)
                        {
                            row[col] = Convert.ToInt32(value);
                        }
                        else
                        {
                            row[col] = value;
                        }
                    }
                }
                dt.Rows.Add(row);
            }

            DataTable = dt;

            // Подписываемся на изменения в DataTable
            DataTable.RowChanged += DataTable_RowChanged;
            DataTable.RowDeleted += DataTable_RowDeleted;
        }

        private void DataTable_RowChanged(object sender, DataRowChangeEventArgs e)
        {
            // Обновляем оригинальные данные при изменении строки
            if (e.Action == DataRowAction.Change)
            {
                UpdateOriginalDataFromRow(e.Row);
            }
        }

        private void DataTable_RowDeleted(object sender, DataRowChangeEventArgs e)
        {
            // При удалении строки из DataTable, удаляем из _originalData
            // Это уже обрабатывается в DeleteSelectedItem
        }

        private void UpdateOriginalDataFromRow(DataRow row)
        {
            try
            {
                // Находим ID записи
                var idColumn = DataTable.Columns.Cast<DataColumn>().FirstOrDefault(c => c.ColumnName.EndsWith("ID") || c.ColumnName == "Id");
                if (idColumn == null) return;

                var id = row[idColumn];

                // Находим соответствующий объект в _originalData
                var originalItem = _originalData.FirstOrDefault(item =>
                {
                    var itemId = _selectedTable.GetId(item);
                    return itemId != null && itemId.Equals(id);
                });

                if (originalItem != null)
                {
                    // Обновляем свойства оригинального объекта
                    var properties = originalItem.GetType().GetProperties();
                    foreach (DataColumn col in DataTable.Columns)
                    {
                        var prop = properties.FirstOrDefault(p => p.Name == col.ColumnName);
                        if (prop != null && prop.CanWrite)
                        {
                            var value = row[col];
                            if (value != DBNull.Value)
                            {
                                // Преобразуем тип если нужно
                                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
                                    var convertedValue = Convert.ChangeType(value, underlyingType);
                                    prop.SetValue(originalItem, convertedValue);
                                }
                                else if (prop.PropertyType.IsEnum)
                                {
                                    var enumValue = Enum.ToObject(prop.PropertyType, value);
                                    prop.SetValue(originalItem, enumValue);
                                }
                                else
                                {
                                    prop.SetValue(originalItem, value);
                                }
                            }
                            else
                            {
                                prop.SetValue(originalItem, null);
                            }
                        }
                    }

                    // Помечаем объект как измененный в контексте
                    var entry = _context.Entry(originalItem);
                    if (entry.State == EntityState.Detached)
                    {
                        // Если объект отсоединен, присоединяем его
                        _context.Set(originalItem.GetType()).Attach(originalItem);
                    }
                    entry.State = EntityState.Modified;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления: {ex.Message}");
            }
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchText) || DataTable == null)
            {
                if (_originalData != null)
                {
                    CreateDataTable(_originalData);
                }
                return;
            }

            try
            {
                string search = SearchText.ToLower();

                var filtered = _originalData.Where(item =>
                {
                    var properties = item.GetType().GetProperties();

                    if (properties.Length > 0)
                    {
                        var val1 = properties[0].GetValue(item)?.ToString() ?? "";
                        if (val1.ToLower().Contains(search))
                            return true;
                    }

                    if (properties.Length > 1)
                    {
                        var val2 = properties[1].GetValue(item)?.ToString() ?? "";
                        if (val2.ToLower().Contains(search))
                            return true;
                    }

                    return false;
                }).ToList();

                CreateDataTable(filtered);
                StatusMessage = $"Найдено: {filtered.Count}";
                StatusColor = "#3498db";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка поиска: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private void SaveChanges(object parameter)
        {
            try
            {
                // Сначала обновляем все измененные строки
                if (DataTable != null)
                {
                    foreach (DataRow row in DataTable.Rows)
                    {
                        if (row.RowState == DataRowState.Modified || row.RowState == DataRowState.Added)
                        {
                            UpdateOriginalDataFromRow(row);
                        }
                    }
                }

                // Сохраняем изменения в контексте
                _context.SaveChanges();
                AuditLogger.Log("BULK_UPDATE", _selectedTable?.Name ?? "Table", $"Сохранены массовые изменения таблицы '{_selectedTable?.Name}'");

                // Перезагружаем данные
                LoadTableData();

                StatusMessage = "Изменения сохранены";
                StatusColor = "#27ae60";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private void RefreshData(object parameter)
        {
            try
            {
                _context?.Dispose();
                _context = new KPMurtazinEntities();
                _context.Configuration.ProxyCreationEnabled = false;
                _context.Configuration.LazyLoadingEnabled = false;

                LoadTableData();
                StatusMessage = "Данные обновлены";
                StatusColor = "#3498db";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка обновления: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private void ResetFilters(object parameter)
        {
            SearchText = "";
            StatusMessage = "Фильтры сброшены";
            StatusColor = "#3498db";
        }

        public void AddNewItem()
        {
            if (_selectedTable == null) return;

            try
            {
                var newItem = Activator.CreateInstance(_selectedTable.EntityType);
                _selectedTable.AddEntity(_context, newItem);
                _originalData.Add(newItem);
                CreateDataTable(_originalData);
                AuditLogger.Log("CREATE", _selectedTable.Name, $"Добавлена новая запись в таблицу '{_selectedTable.Name}'");

                StatusMessage = "Новая запись добавлена";
                StatusColor = "#27ae60";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка добавления: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        public void DeleteSelectedItem(object selectedItem)
        {
            if (selectedItem == null || _selectedTable == null) return;

            try
            {
                // Получаем ID из выбранной строки DataRowView
                if (selectedItem is DataRowView rowView)
                {
                    var idColumn = DataTable.Columns.Cast<DataColumn>().FirstOrDefault(c => c.ColumnName.EndsWith("ID") || c.ColumnName == "Id");
                    if (idColumn != null)
                    {
                        var id = rowView.Row[idColumn];

                        var originalItem = _originalData.FirstOrDefault(item =>
                        {
                            var itemId = _selectedTable.GetId(item);
                            return itemId != null && itemId.Equals(id);
                        });

                        if (originalItem != null)
                        {
                            _selectedTable.RemoveEntity(_context, originalItem);
                            _originalData.Remove(originalItem);
                            AuditLogger.Log("DELETE", _selectedTable.Name, $"Удалена запись из таблицы '{_selectedTable.Name}'", id?.ToString());
                        }
                    }
                }

                CreateDataTable(_originalData);
                StatusMessage = "Запись удалена";
                StatusColor = "#27ae60";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка удаления: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        public void DisposeContext()
        {
            _context?.Dispose();
        }
    }
}