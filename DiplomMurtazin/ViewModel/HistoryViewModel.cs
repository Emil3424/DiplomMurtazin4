using DiplomMurtazin.Core;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace DiplomMurtazin.ViewModel
{
    public class HistoryItem
    {
        public int AuditID { get; set; }
        public DateTime EventDate { get; set; }
        public string UserLogin { get; set; }
        public int? EmployeeID { get; set; }
        public string ActionType { get; set; }
        public string EntityType { get; set; }
        public string EntityID { get; set; }
        public string Details { get; set; }
        public string Metadata { get; set; }
    }

    public class HistoryViewModel : BaseViewModel
    {
        private ObservableCollection<HistoryItem> _historyItems;
        private ObservableCollection<HistoryItem> _filteredItems;
        private string _searchText;
        private string _selectedActionType = "Все";
        private string _selectedEntityType = "Все";
        private DateTime? _startDate = DateTime.Today.AddMonths(-1);
        private DateTime? _endDate = DateTime.Today;
        private string _statusMessage = "Готов к работе";
        private string _statusColor = "#3498db";

        public ObservableCollection<HistoryItem> FilteredItems
        {
            get => _filteredItems;
            set => Set(ref _filteredItems, value);
        }

        public ObservableCollection<string> ActionTypes { get; } = new ObservableCollection<string> { "Все" };
        public ObservableCollection<string> EntityTypes { get; } = new ObservableCollection<string> { "Все" };

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

        public string SelectedActionType
        {
            get => _selectedActionType;
            set
            {
                if (Set(ref _selectedActionType, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string SelectedEntityType
        {
            get => _selectedEntityType;
            set
            {
                if (Set(ref _selectedEntityType, value))
                {
                    ApplyFilters();
                }
            }
        }

        public DateTime? StartDate
        {
            get => _startDate;
            set => Set(ref _startDate, value);
        }

        public DateTime? EndDate
        {
            get => _endDate;
            set => Set(ref _endDate, value);
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

        public ICommand LoadCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ApplyFilterCommand { get; }
        public ICommand ResetFiltersCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand ExportExcelCommand { get; }
        public ICommand ExportPdfCommand { get; }

        public HistoryViewModel()
        {
            _historyItems = new ObservableCollection<HistoryItem>();
            _filteredItems = new ObservableCollection<HistoryItem>();

            LoadCommand = new RelayCommand(_ => LoadData());
            RefreshCommand = new RelayCommand(_ => LoadData());
            ApplyFilterCommand = new RelayCommand(_ => ApplyFilters());
            ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
            ExportCsvCommand = new RelayCommand(_ => ExportCsv(), _ => FilteredItems.Any());
            ExportExcelCommand = new RelayCommand(_ => ExportExcel(), _ => FilteredItems.Any());
            ExportPdfCommand = new RelayCommand(_ => ExportPdf(), _ => FilteredItems.Any());
        }

        private void LoadData()
        {
            try
            {
                using (var context = new KPMurtazinEntities())
                {
                    const string sql = @"
SELECT AuditID, EventDate, UserLogin, EmployeeID, ActionType, EntityType, EntityID, Details, Metadata
FROM dbo.AuditLog
ORDER BY EventDate DESC";

                    var data = context.Database.SqlQuery<HistoryItem>(sql).ToList();
                    _historyItems = new ObservableCollection<HistoryItem>(data);
                }

                UpdateFilterValues();
                ApplyFilters();
                StatusMessage = $"Загружено событий: {_historyItems.Count}";
                StatusColor = "#27ae60";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки истории: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private void ApplyFilters()
        {
            var query = _historyItems.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string search = SearchText.ToLower();
                query = query.Where(x =>
                    (x.UserLogin ?? "").ToLower().Contains(search) ||
                    (x.ActionType ?? "").ToLower().Contains(search) ||
                    (x.EntityType ?? "").ToLower().Contains(search) ||
                    (x.Details ?? "").ToLower().Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(SelectedActionType) && SelectedActionType != "Все")
            {
                query = query.Where(x => string.Equals(x.ActionType, SelectedActionType, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SelectedEntityType) && SelectedEntityType != "Все")
            {
                query = query.Where(x => string.Equals(x.EntityType, SelectedEntityType, StringComparison.OrdinalIgnoreCase));
            }

            if (StartDate.HasValue)
            {
                query = query.Where(x => x.EventDate >= StartDate.Value.Date);
            }

            if (EndDate.HasValue)
            {
                query = query.Where(x => x.EventDate < EndDate.Value.Date.AddDays(1));
            }

            FilteredItems = new ObservableCollection<HistoryItem>(query.OrderByDescending(x => x.EventDate));
            StatusMessage = $"Показано событий: {FilteredItems.Count}";
            StatusColor = "#3498db";
        }

        private void ResetFilters()
        {
            SearchText = string.Empty;
            SelectedActionType = "Все";
            SelectedEntityType = "Все";
            StartDate = DateTime.Today.AddMonths(-1);
            EndDate = DateTime.Today;
            ApplyFilters();
        }

        private void UpdateFilterValues()
        {
            var actions = _historyItems.Select(x => x.ActionType).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList();
            var entities = _historyItems.Select(x => x.EntityType).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList();

            ActionTypes.Clear();
            ActionTypes.Add("Все");
            foreach (var a in actions) ActionTypes.Add(a);

            EntityTypes.Clear();
            EntityTypes.Add("Все");
            foreach (var e in entities) EntityTypes.Add(e);
        }

        private void ExportCsv()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"История_операций_{DateTime.Now:yyyyMMddHHmmss}.csv",
                DefaultExt = ".csv"
            };

            if (dialog.ShowDialog() == true)
            {
                using (var writer = new StreamWriter(dialog.FileName, false, Encoding.UTF8))
                {
                    writer.WriteLine("Дата;Пользователь;Действие;Сущность;ID сущности;Описание");
                    foreach (var row in FilteredItems)
                    {
                        writer.WriteLine($"{row.EventDate:dd.MM.yyyy HH:mm:ss};{Escape(row.UserLogin)};{Escape(row.ActionType)};{Escape(row.EntityType)};{Escape(row.EntityID)};{Escape(row.Details)}");
                    }
                }

                StatusMessage = $"История сохранена: {Path.GetFileName(dialog.FileName)}";
                StatusColor = "#27ae60";
            }
        }

        private void ExportExcel()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel files (*.xls)|*.xls",
                FileName = $"История_операций_{DateTime.Now:yyyyMMddHHmmss}.xls",
                DefaultExt = ".xls"
            };

            if (dialog.ShowDialog() == true)
            {
                using (var writer = new StreamWriter(dialog.FileName, false, Encoding.Unicode))
                {
                    writer.WriteLine("Дата\tПользователь\tДействие\tСущность\tID сущности\tОписание");
                    foreach (var row in FilteredItems)
                    {
                        writer.WriteLine($"{row.EventDate:dd.MM.yyyy HH:mm:ss}\t{row.UserLogin}\t{row.ActionType}\t{row.EntityType}\t{row.EntityID}\t{row.Details}");
                    }
                }

                StatusMessage = $"История сохранена: {Path.GetFileName(dialog.FileName)}";
                StatusColor = "#27ae60";
            }
        }

        private void ExportPdf()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = $"История_операций_{DateTime.Now:yyyyMMddHHmmss}.pdf",
                DefaultExt = ".pdf"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            using (var document = new PdfDocument())
            {
                var page = document.AddPage();
                page.Width = XUnit.FromPoint(595);
                page.Height = XUnit.FromPoint(842);
                var gfx = XGraphics.FromPdfPage(page);
                var font = new XFont("Arial", 9, XFontStyleEx.Regular);
                var title = new XFont("Arial", 13, XFontStyleEx.Bold);
                double y = 30;

                gfx.DrawString("ИСТОРИЯ ОПЕРАЦИЙ", title, XBrushes.DarkBlue, 30, y);
                y += 20;

                foreach (var row in FilteredItems.Take(300))
                {
                    if (y > page.Height.Point - 25)
                    {
                        page = document.AddPage();
                        page.Width = XUnit.FromPoint(595);
                        page.Height = XUnit.FromPoint(842);
                        gfx.Dispose();
                        gfx = XGraphics.FromPdfPage(page);
                        y = 30;
                    }

                    string line = $"{row.EventDate:dd.MM HH:mm} | {row.UserLogin} | {row.ActionType} | {row.EntityType} | {row.Details}";
                    gfx.DrawString(Truncate(line, 105), font, XBrushes.Black, 30, y);
                    y += 14;
                }

                gfx.Dispose();
                document.Save(dialog.FileName);
            }

            StatusMessage = $"История сохранена: {Path.GetFileName(dialog.FileName)}";
            StatusColor = "#27ae60";
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (value.Contains(";") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max)
            {
                return value;
            }

            return value.Substring(0, max - 3) + "...";
        }
    }
}
