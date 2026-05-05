using DiplomMurtazin.Core;
using DiplomMurtazin.Model;
using DiplomMurtazin.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace DiplomMurtazin.ViewModel
{
    public class TableColumnInfo : BaseViewModel
    {
        private string _name;
        private string _displayName;
        private string _typeName;
        private bool _isSelected;
        private bool _isPrimaryKey;

        public string Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        public string DisplayName
        {
            get => _displayName;
            set => Set(ref _displayName, value);
        }

        public string TypeName
        {
            get => _typeName;
            set => Set(ref _typeName, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (Set(ref _isSelected, value))
                {
                    // При изменении выбора, уведомляем что свойство изменилось
                    // Это заставит интерфейс обновиться
                }
            }
        }

        public bool IsPrimaryKey
        {
            get => _isPrimaryKey;
            set => Set(ref _isPrimaryKey, value);
        }
    }

    public class ViewSettingsViewModel : BaseViewModel
    {
        private KPMurtazinEntities _context;
        private ObservableCollection<TableInfo> _availableTables;
        private TableInfo _selectedTable;
        private ObservableCollection<TableColumnInfo> _tableColumns;
        private ObservableCollection<SavedViewInfo> _savedViews;
        private SavedViewInfo _selectedView;
        private string _viewName;
        private string _tableInfo;
        private string _statusMessage;
        private string _statusColor;


        public ObservableCollection<TableInfo> AvailableTables
        {
            get => _availableTables;
            set => Set(ref _availableTables, value);
        }

        public TableInfo SelectedTable
        {
            get => _selectedTable;
            set
            {
                if (Set(ref _selectedTable, value))
                {
                    LoadTableColumns();
                    UpdateTableInfo();
                }
            }
        }

        public ObservableCollection<TableColumnInfo> TableColumns
        {
            get => _tableColumns;
            set => Set(ref _tableColumns, value);
        }

        public ObservableCollection<SavedViewInfo> SavedViews
        {
            get => _savedViews;
            set => Set(ref _savedViews, value);
        }

        public SavedViewInfo SelectedView
        {
            get => _selectedView;
            set
            {
                if (Set(ref _selectedView, value))
                {
                    LoadSavedView();
                }
            }
        }

        public string ViewName
        {
            get => _viewName;
            set => Set(ref _viewName, value);
        }

        public string TableInfo
        {
            get => _tableInfo;
            set => Set(ref _tableInfo, value);
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
        public ICommand SelectAllCommand { get; }
        public ICommand DeselectAllCommand { get; }
        public ICommand SaveViewCommand { get; }
        public ICommand ApplyViewCommand { get; }
        public ICommand DeleteViewCommand { get; }

        public ViewSettingsViewModel()
        {
            LoadedCommand = new RelayCommand(OnLoaded);
            SelectAllCommand = new RelayCommand(SelectAll);
            DeselectAllCommand = new RelayCommand(DeselectAll);
            SaveViewCommand = new RelayCommand(SaveView, CanSaveView);
            ApplyViewCommand = new RelayCommand(ApplyView, CanApplyView);
            DeleteViewCommand = new RelayCommand(DeleteView, CanDeleteView);
            ViewDisplayCommand = new RelayCommand(ViewDisplay, CanViewDisplay);


            StatusColor = "#3498db";
            StatusMessage = "Готов к работе";
        }

        private void OnLoaded(object parameter)
        {
            _context = new KPMurtazinEntities();
            _context.Configuration.ProxyCreationEnabled = false;

            LoadAvailableTables();
            LoadSavedViews();
        }

        private void LoadAvailableTables()
        {
            AvailableTables = new ObservableCollection<TableInfo>
            {
                new TableInfo { Name = "Категории", EntityType = typeof(Categories) },
                new TableInfo { Name = "Должности", EntityType = typeof(Positions) },
                new TableInfo { Name = "Поставщики", EntityType = typeof(Suppliers) },
                new TableInfo { Name = "Складские зоны", EntityType = typeof(WarehouseZones) },
                new TableInfo { Name = "Пользователи", EntityType = typeof(Users) },
                new TableInfo { Name = "Сотрудники", EntityType = typeof(Employees) },
                new TableInfo { Name = "Товары", EntityType = typeof(Products) },
                new TableInfo { Name = "Продажи", EntityType = typeof(Sales) },
                new TableInfo { Name = "Позиции продаж", EntityType = typeof(SaleItems) },
                new TableInfo { Name = "Акты инвентаризации", EntityType = typeof(InventoryActs) },
                new TableInfo { Name = "Детали инвентаризации", EntityType = typeof(InventoryDetails) },
                new TableInfo { Name = "Накладные", EntityType = typeof(Invoices) },
                new TableInfo { Name = "Позиции накладных", EntityType = typeof(InvoiceItems) },
                new TableInfo { Name = "Смены", EntityType = typeof(Shifts) },
                new TableInfo { Name = "Остатки", EntityType = typeof(StockBalances) },
                new TableInfo { Name = "Зарплаты", EntityType = typeof(Salaries) },
                new TableInfo { Name = "История движения", EntityType = typeof(ProductMovementHistory) }
            };
        }

        private void LoadSavedViews()
        {
            try
            {
                // Создаем таблицу, если её нет
                EnsureViewTableExists();

                var views = _context.UserViews.OrderBy(v => v.Name).ToList();
                SavedViews = new ObservableCollection<SavedViewInfo>(
                    views.Select(v => new SavedViewInfo
                    {
                        Id = v.Id,
                        Name = v.Name,
                        TableName = v.TableName
                    })
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки представлений: {ex.Message}");
                SavedViews = new ObservableCollection<SavedViewInfo>();
            }
        }

        private void LoadSavedView()
        {
            if (SelectedView == null) return;

            try
            {
                var view = _context.UserViews.Find(SelectedView.Id);
                if (view != null)
                {
                    // Выбираем таблицу, если она еще не выбрана
                    var table = AvailableTables.FirstOrDefault(t => t.Name == view.TableName);
                    if (table != null && SelectedTable != table)
                    {
                        SelectedTable = table;
                    }

                    // Применяем выбранные колонки
                    var columns = view.GetColumnList();
                    foreach (var col in TableColumns)
                    {
                        col.IsSelected = columns.Contains(col.Name);
                    }

                    UpdateTableInfo();
                    ViewName = view.Name;
                    StatusMessage = "Представление загружено";
                    StatusColor = "#27ae60";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private void LoadTableColumns()
        {
            if (SelectedTable == null || SelectedTable.EntityType == null)
            {
                TableColumns = new ObservableCollection<TableColumnInfo>();
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Загрузка колонок для: {SelectedTable.Name}");

            var columns = new ObservableCollection<TableColumnInfo>();
            var properties = SelectedTable.EntityType.GetProperties();

            foreach (var prop in properties)
            {
                // Пропускаем навигационные свойства
                if (prop.PropertyType.IsGenericType &&
                    (prop.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                     prop.PropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                    continue;

                // Пропускаем сложные типы
                if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string) &&
                    !prop.PropertyType.IsGenericType)
                    continue;

                string typeName = GetSimpleTypeName(prop.PropertyType);
                bool isPrimaryKey = prop.Name.EndsWith("ID") || prop.Name == "Id";

                System.Diagnostics.Debug.WriteLine($"  Колонка: {prop.Name} ({typeName})");

                columns.Add(new TableColumnInfo
                {
                    Name = prop.Name,
                    DisplayName = GetDisplayName(prop.Name),
                    TypeName = typeName,
                    IsSelected = isPrimaryKey,
                    IsPrimaryKey = isPrimaryKey
                });
            }

            // Сортируем: сначала ID, потом остальные
            TableColumns = new ObservableCollection<TableColumnInfo>(
                columns.OrderByDescending(c => c.IsPrimaryKey)
                       .ThenBy(c => c.DisplayName)
            );

            UpdateTableInfo();
        }

        private string GetDisplayName(string propName)
        {
            // Преобразуем CamelCase в читаемый вид
            return System.Text.RegularExpressions.Regex.Replace(propName, "([a-z])([A-Z])", "$1 $2");
        }

        private string GetSimpleTypeName(Type type)
        {
            if (type == typeof(string)) return "текст";
            if (type == typeof(int)) return "число";
            if (type == typeof(decimal)) return "сумма";
            if (type == typeof(DateTime)) return "дата";
            if (type == typeof(bool)) return "да/нет";
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var underlying = Nullable.GetUnderlyingType(type);
                return GetSimpleTypeName(underlying) + "?";
            }
            return "другое";
        }

        private void UpdateTableInfo()
        {
            if (SelectedTable == null)
            {
                TableInfo = "Выберите таблицу для настройки";
                return;
            }

            int totalColumns = TableColumns?.Count ?? 0;
            int selectedCount = TableColumns?.Count(c => c.IsSelected) ?? 0;
            TableInfo = $"Всего полей: {totalColumns}, выбрано: {selectedCount}";
        }

        private void SelectAll(object parameter)
        {
            if (TableColumns == null) return;

            // Создаем новый список для принудительного обновления
            var newColumns = new ObservableCollection<TableColumnInfo>();

            foreach (var col in TableColumns)
            {
                col.IsSelected = true;
                newColumns.Add(col);
            }

            // Принудительно обновляем коллекцию
            TableColumns = newColumns;
            UpdateTableInfo();

            // Дополнительно вызываем обновление интерфейса
            OnPropertyChanged(nameof(TableColumns));
        }

        private void DeselectAll(object parameter)
        {
            if (TableColumns == null) return;

            var newColumns = new ObservableCollection<TableColumnInfo>();

            foreach (var col in TableColumns)
            {
                // Оставляем выбранными только ID
                col.IsSelected = col.IsPrimaryKey;
                newColumns.Add(col);
            }

            TableColumns = newColumns;
            UpdateTableInfo();
            OnPropertyChanged(nameof(TableColumns));
        }
        public ICommand ViewDisplayCommand { get; }

        private bool CanViewDisplay(object parameter)
        {
            return SelectedView != null;
        }

        private void ViewDisplay(object parameter)
        {
            if (SelectedView == null) return;

            try
            {
                // Находим главное окно и его Frame
                var mainWindow = Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(w => w is MainWindow) as MainWindow;

                if (mainWindow != null)
                {
                    // Создаем ViewDisplayPage и передаем ID выбранного представления
                    var displayPage = new ViewDisplayPage(SelectedView.Id);
                    mainWindow.MainFrame.Navigate(displayPage);

                    StatusMessage = "Переход к просмотру";
                    StatusColor = "#27ae60";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка перехода: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }
        private bool CanSaveView(object parameter)
        {
            return SelectedTable != null &&
                   TableColumns != null &&
                   TableColumns.Any(c => c.IsSelected) &&
                   !string.IsNullOrWhiteSpace(ViewName);
        }

        private void SaveView(object parameter)
        {
            try
            {
                // Создаем таблицу для представлений, если её нет
                EnsureViewTableExists();

                var selectedColumns = TableColumns.Where(c => c.IsSelected)
                                                  .Select(c => c.Name)
                                                  .ToList();

                var view = new UserViews
                {
                    Name = ViewName,
                    TableName = SelectedTable.Name,
                    CreatedDate = DateTime.Now
                };
                view.SetColumnList(selectedColumns);

                _context.UserViews.Add(view);
                _context.SaveChanges();

                // Обновляем список сохраненных представлений
                LoadSavedViews();

                // Получаем ID только что созданного представления
                int newViewId = view.Id;

                StatusMessage = "Представление сохранено";
                StatusColor = "#27ae60";

                // Очищаем поле названия
                ViewName = "";

                // АВТОМАТИЧЕСКИЙ ПЕРЕХОД К ПРОСМОТРУ
                OpenViewDisplay(newViewId);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private void OpenViewDisplay(int viewId)
        {
            try
            {
                // Находим главное окно
                var mainWindow = Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(w => w is MainWindow) as MainWindow;

                if (mainWindow != null)
                {
                    // Создаем страницу просмотра с ID представления
                    var displayPage = new ViewDisplayPage(viewId);

                    // Переходим на страницу просмотра
                    mainWindow.MainFrame.Navigate(displayPage);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка перехода: {ex.Message}");
            }
        }

        private bool CanApplyView(object parameter)
        {
            return SelectedView != null && SelectedTable != null;
        }

        private void ApplyView(object parameter)
        {
            if (SelectedView == null || SelectedTable == null) return;

            try
            {
                var view = _context.UserViews.Find(SelectedView.Id);
                if (view != null && view.TableName == SelectedTable.Name)
                {
                    var columns = view.GetColumnList();

                    foreach (var col in TableColumns)
                    {
                        col.IsSelected = columns.Contains(col.Name);
                    }

                    UpdateTableInfo();
                    StatusMessage = "Представление применено";
                    StatusColor = "#27ae60";
                }
                else
                {
                    StatusMessage = "Представление не подходит для текущей таблицы";
                    StatusColor = "#e74c3c";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка применения: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private bool CanDeleteView(object parameter)
        {
            return SelectedView != null;
        }

        private void DeleteView(object parameter)
        {
            if (SelectedView == null) return;

            try
            {
                var view = _context.UserViews.Find(SelectedView.Id);
                if (view != null)
                {
                    _context.UserViews.Remove(view);
                    _context.SaveChanges();

                    LoadSavedViews();
                    SelectedView = null;

                    StatusMessage = "Представление удалено";
                    StatusColor = "#27ae60";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка удаления: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private void EnsureViewTableExists()
        {
            try
            {
                // Проверяем, существует ли таблица
                _context.UserViews.FirstOrDefault();
            }
            catch
            {
                // Создаем таблицу
                _context.Database.ExecuteSqlCommand(@"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserViews' AND xtype='U')
                    CREATE TABLE [dbo].[UserViews](
                        [Id] [int] IDENTITY(1,1) NOT NULL,
                        [Name] [nvarchar](100) NOT NULL,
                        [TableName] [nvarchar](100) NOT NULL,
                        [SelectedColumns] [nvarchar](max) NOT NULL,
                        [CreatedDate] [datetime2](7) NOT NULL,
                        CONSTRAINT [PK_UserViews] PRIMARY KEY CLUSTERED ([Id] ASC)
                    )");

                // Обновляем модель (потребуется перезапуск)
                // Но для этого запуска просто проигнорируем
            }
        }

        public void DisposeContext()
        {
            _context?.Dispose();
        }
    }
}