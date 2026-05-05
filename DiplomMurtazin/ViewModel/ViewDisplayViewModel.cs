using DiplomMurtazin.Core;
using DiplomMurtazin.Model;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace DiplomMurtazin.ViewModel
{
    public class ViewDisplayViewModel : BaseViewModel
    {
        private KPMurtazinEntities _context;
        private ObservableCollection<SavedViewInfo> _savedViews;
        private SavedViewInfo _selectedView;
        private ObservableCollection<object> _viewData;
        private List<string> _columnNames;
        private bool _hasData;
        private string _viewInfo;
        private string _statusMessage;
        private string _statusColor;

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
                    UpdateViewInfo();
                }
            }
        }

        public ObservableCollection<object> ViewData
        {
            get => _viewData;
            set => Set(ref _viewData, value);
        }

        public List<string> ColumnNames
        {
            get => _columnNames;
            set => Set(ref _columnNames, value);
        }

        public bool HasData
        {
            get => _hasData;
            set => Set(ref _hasData, value);
        }

        public string ViewInfo
        {
            get => _viewInfo;
            set => Set(ref _viewInfo, value);
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
        public ICommand RefreshCommand { get; }
        public ICommand LoadViewCommand { get; }
        public ICommand ExportToExcelCommand { get; }

        public ViewDisplayViewModel()
        {
            LoadedCommand = new RelayCommand(OnLoaded);
            RefreshCommand = new RelayCommand(Refresh);
            LoadViewCommand = new RelayCommand(LoadView, CanLoadView);
            ExportToExcelCommand = new RelayCommand(ExportToExcel, CanExportToExcel);

            StatusColor = "#3498db";
            StatusMessage = "Готов к работе";

            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Уведомляем View об изменении колонок
        }

        private void OnLoaded(object parameter)
        {
            _context = new KPMurtazinEntities();
            _context.Configuration.ProxyCreationEnabled = false;

            LoadSavedViews();
        }

        private void LoadSavedViews()
        {
            try
            {
                var views = _context.UserViews.OrderBy(v => v.Name).ToList();
                SavedViews = new ObservableCollection<SavedViewInfo>(
                    views.Select(v => new SavedViewInfo
                    {
                        Id = v.Id,
                        Name = v.Name,
                        TableName = v.TableName
                    })
                );

                if (SavedViews.Any())
                {
                    StatusMessage = $"Загружено представлений: {SavedViews.Count}";
                    StatusColor = "#3498db";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private void UpdateViewInfo()
        {
            if (SelectedView == null)
            {
                ViewInfo = "";
                return;
            }

            ViewInfo = $"Таблица: {SelectedView.TableName}";
        }

        private bool CanLoadView(object parameter)
        {
            return SelectedView != null;
        }

        private void LoadView(object parameter)
        {
            try
            {
                if (SelectedView == null)
                {
                    StatusMessage = "Выберите представление";
                    StatusColor = "#e74c3c";
                    return;
                }

                var view = _context.UserViews.Find(SelectedView.Id);
                if (view == null)
                {
                    StatusMessage = "Представление не найдено";
                    StatusColor = "#e74c3c";
                    return;
                }

                // Получаем список колонок для отображения
                var columns = view.GetColumnList();
                if (columns == null || columns.Count == 0)
                {
                    StatusMessage = "В представлении нет выбранных колонок";
                    StatusColor = "#e74c3c";
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Загрузка таблицы: {view.TableName}");
                System.Diagnostics.Debug.WriteLine($"Колонки: {string.Join(", ", columns)}");

                // Загружаем данные из соответствующей таблицы
                var data = LoadTableData(view.TableName);

                // Создаем список для хранения отображаемых имен колонок
                var displayColumnNames = columns.Select(c => GetDisplayName(c)).ToList();
                ColumnNames = displayColumnNames;

                if (data == null || !data.Any())
                {
                    StatusMessage = $"Нет данных в таблице {view.TableName}";
                    StatusColor = "#e74c3c";

                    // Создаем одну пустую строку для отображения заголовков
                    var emptyResult = new ObservableCollection<object>();
                    var emptyDict = new Dictionary<string, object>();

                    foreach (var col in displayColumnNames)
                    {
                        emptyDict[col] = "";
                    }

                    // Используем Dictionary напрямую вместо ExpandoObject
                    emptyResult.Add(emptyDict);

                    ViewData = emptyResult;
                    HasData = false;
                    return;
                }

                // Создаем коллекцию для результатов
                var result = new ObservableCollection<object>();

                foreach (var item in data)
                {
                    // Используем Dictionary для хранения значений
                    var dict = new Dictionary<string, object>();

                    foreach (var col in columns)
                    {
                        var prop = item.GetType().GetProperty(col);
                        if (prop != null)
                        {
                            var value = prop.GetValue(item);
                            var displayName = GetDisplayName(col);

                            // Форматируем значения для отображения
                            if (value == null)
                            {
                                dict[displayName] = "—";
                            }
                            else if (value is DateTime date)
                            {
                                dict[displayName] = date.ToString("dd.MM.yyyy HH:mm");
                            }
                            else if (value is decimal dec)
                            {
                                dict[displayName] = dec.ToString("F2");
                            }
                            else if (value is bool b)
                            {
                                dict[displayName] = b ? "Да" : "Нет";
                            }
                            else
                            {
                                dict[displayName] = value.ToString();
                            }
                        }
                        else
                        {
                            dict[GetDisplayName(col)] = "—";
                        }
                    }

                    result.Add(dict);
                }

                ViewData = result;
                HasData = true;

                StatusMessage = $"Загружено записей: {result.Count}";
                StatusColor = "#27ae60";

                System.Diagnostics.Debug.WriteLine($"Загружено записей: {result.Count}");

                // Принудительно обновляем интерфейс
                OnPropertyChanged(nameof(ViewData));
                OnPropertyChanged(nameof(ColumnNames));
                OnPropertyChanged(nameof(HasData));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки данных: {ex.Message}";
                StatusColor = "#e74c3c";
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex}");
            }
        }
        private bool CanExportToExcel(object parameter)
        {
            return HasData && ViewData != null && ViewData.Count > 0;
        }

        private void ExportToExcel(object parameter)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.csv)|*.csv|Все файлы (*.*)|*.*",
                    FileName = $"Экспорт_{SelectedView?.Name}_{DateTime.Now:yyyyMMddHHmmss}.csv",
                    DefaultExt = ".csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    ExportToCsv(dialog.FileName);
                    StatusMessage = $"Данные экспортированы в {Path.GetFileName(dialog.FileName)}";
                    StatusColor = "#27ae60";

                    // Спрашиваем, открыть ли файл
                    var result = MessageBox.Show("Файл сохранен. Открыть его?",
                        "Экспорт завершен",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(dialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка экспорта: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private void ExportToCsv(string filePath)
        {
            var sb = new StringBuilder();

            // Заголовки
            sb.AppendLine(string.Join(";", ColumnNames));

            // Данные
            foreach (System.Collections.IDictionary item in ViewData)
            {
                var row = new List<string>();
                foreach (var colName in ColumnNames)
                {
                    if (item.Contains(colName))
                    {
                        var value = item[colName]?.ToString() ?? "";
                        // Экранируем кавычки и точки с запятой
                        if (value.Contains(";") || value.Contains("\"") || value.Contains("\n"))
                        {
                            value = "\"" + value.Replace("\"", "\"\"") + "\"";
                        }
                        row.Add(value);
                    }
                    else
                    {
                        row.Add("");
                    }
                }
                sb.AppendLine(string.Join(";", row));
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
        private IEnumerable<object> LoadTableData(string tableName)
        {
            switch (tableName)
            {
                case "Категории":
                    return _context.Categories.ToList();
                case "Должности":
                    return _context.Positions.ToList();
                case "Поставщики":
                    return _context.Suppliers.ToList();
                case "Складские зоны":
                    return _context.WarehouseZones.ToList();
                case "Пользователи":
                    return _context.Users.ToList();
                case "Сотрудники":
                    return _context.Employees.ToList();
                case "Товары":
                    return _context.Products.ToList();
                case "Продажи":
                    return _context.Sales.ToList();
                case "Позиции продаж":
                    return _context.SaleItems.ToList();
                case "Акты инвентаризации":
                    return _context.InventoryActs.ToList();
                case "Детали инвентаризации":
                    return _context.InventoryDetails.ToList();
                case "Накладные":
                    return _context.Invoices.ToList();
                case "Позиции накладных":
                    return _context.InvoiceItems.ToList();
                case "Смены":
                    return _context.Shifts.ToList();
                case "Остатки":
                    return _context.StockBalances.ToList();
                case "Зарплаты":
                    return _context.Salaries.ToList();
                case "История движения":
                    return _context.ProductMovementHistory.ToList();
                default:
                    return new List<object>();
            }
        }

        private string GetDisplayName(string propName)
        {
            // Преобразуем CamelCase в читаемый вид
            return System.Text.RegularExpressions.Regex.Replace(propName, "([a-z])([A-Z])", "$1 $2");
        }

        private void Refresh(object parameter)
        {
            LoadSavedViews();
            if (SelectedView != null)
            {
                LoadView(null);
            }
            else
            {
                ViewData = null;
                HasData = false;
                StatusMessage = "Данные обновлены";
                StatusColor = "#3498db";
            }
        }

        public void DisposeContext()
        {
            _context?.Dispose();
        }
    }
}