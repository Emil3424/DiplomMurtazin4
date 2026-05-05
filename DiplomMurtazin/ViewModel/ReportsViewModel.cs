using DiplomMurtazin.Core;
using DiplomMurtazin.Model;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace DiplomMurtazin.ViewModel
{
    public class ReportsViewModel : BaseViewModel
    {
        private KPMurtazinEntities _context;
        private ObservableCollection<SalesReportItem> _salesReport;
        private ObservableCollection<SalesSummaryItem> _salesSummary;
        private ObservableCollection<StockReportItem> _stockReport;
        private DateTime _startDate;
        private DateTime _endDate;
        private string _statusMessage;
        private string _statusColor;
        public int TotalProducts => StockReport?.Count ?? 0;
        public int OutOfStockCount => StockReport?.Count(s => s.CurrentStock == 0) ?? 0;
        public int BelowMinCount => StockReport?.Count(s => s.Status == "Ниже минимума") ?? 0;

        public ObservableCollection<SalesReportItem> SalesReport
        {
            get => _salesReport;
            set => Set(ref _salesReport, value);
        }

        public ObservableCollection<SalesSummaryItem> SalesSummary
        {
            get => _salesSummary;
            set => Set(ref _salesSummary, value);
        }

        public ObservableCollection<StockReportItem> StockReport
        {
            get => _stockReport;
            set => Set(ref _stockReport, value);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set => Set(ref _startDate, value);
        }

        public DateTime EndDate
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

        public ICommand LoadedCommand { get; }
        public ICommand GenerateSalesReportCommand { get; }
        public ICommand GenerateStockReportCommand { get; }
        public ICommand ExportSalesReportPdfCommand { get; }
        public ICommand ExportSalesReportCsvCommand { get; }
        public ICommand ExportSalesReportExcelCommand { get; }
        public ICommand ExportStockReportPdfCommand { get; }
        public ICommand ExportStockReportCsvCommand { get; }
        public ICommand ExportStockReportExcelCommand { get; }
        public ICommand ExportProductCatalogPdfCommand { get; }
        public ICommand ExportProductCatalogCsvCommand { get; }
        public ICommand ExportProductCatalogExcelCommand { get; }

        public ReportsViewModel()
        {
            LoadedCommand = new RelayCommand(OnLoaded);
            GenerateSalesReportCommand = new RelayCommand(GenerateSalesReport);
            GenerateStockReportCommand = new RelayCommand(GenerateStockReport);
            ExportSalesReportPdfCommand = new RelayCommand(ExportSalesReportPdf, CanExportSalesReport);
            ExportSalesReportCsvCommand = new RelayCommand(ExportSalesReportCsv, CanExportSalesReport);
            ExportSalesReportExcelCommand = new RelayCommand(ExportSalesReportExcel, CanExportSalesReport);
            ExportStockReportPdfCommand = new RelayCommand(ExportStockReportPdf, CanExportStockReport);
            ExportStockReportCsvCommand = new RelayCommand(ExportStockReportCsv, CanExportStockReport);
            ExportStockReportExcelCommand = new RelayCommand(ExportStockReportExcel, CanExportStockReport);
            ExportProductCatalogPdfCommand = new RelayCommand(ExportProductCatalogPdf);
            ExportProductCatalogCsvCommand = new RelayCommand(ExportProductCatalogCsv);
            ExportProductCatalogExcelCommand = new RelayCommand(ExportProductCatalogExcel);

            StartDate = DateTime.Now.AddMonths(-1);
            EndDate = DateTime.Now;

            StatusColor = "#3498db";
            StatusMessage = "Готов к работе";
        }

        private void OnLoaded(object parameter)
        {
            _context = new KPMurtazinEntities();
            _context.Configuration.ProxyCreationEnabled = false;
        }

        private void GenerateSalesReport(object parameter)
        {
            try
            {
                var sales = _context.Sales
                    .Include(s => s.Employees)
                    .Include(s => s.SaleItems)
                    .Where(s => s.SaleDateTime >= StartDate && s.SaleDateTime <= EndDate)
                    .ToList();

                SalesReport = new ObservableCollection<SalesReportItem>(
                    sales.Select(s => new SalesReportItem
                    {
                        SaleId = s.SaleID,
                        SaleDate = s.SaleDateTime,
                        EmployeeName = s.Employees != null ?
                            $"{s.Employees.LastName} {s.Employees.FirstName}" : "Неизвестно",
                        PaymentMethod = s.PaymentMethod,
                        ItemsCount = s.SaleItems.Count,
                        TotalAmount = s.TotalAmount
                    }).OrderByDescending(s => s.SaleDate)
                );

                // Группировка по сотрудникам
                SalesSummary = new ObservableCollection<SalesSummaryItem>(
                    sales.GroupBy(s => s.EmployeeID)
                         .Select(g => new SalesSummaryItem
                         {
                             EmployeeName = g.First().Employees != null ?
                                 $"{g.First().Employees.LastName} {g.First().Employees.FirstName}" : "Неизвестно",
                             SalesCount = g.Count(),
                             TotalAmount = g.Sum(s => s.TotalAmount),
                             AverageCheck = g.Count() > 0 ? g.Sum(s => s.TotalAmount) / g.Count() : 0
                         }).OrderByDescending(s => s.TotalAmount)
                );

                StatusMessage = $"Отчет по продажам сгенерирован. Продаж: {SalesReport.Count}";
                StatusColor = "#27ae60";
                AuditLogger.Log("REPORT_GENERATE", "SalesReport", $"Сформирован отчет по продажам за {StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}", metadata: $"Rows={SalesReport.Count}");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка генерации отчета: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private void GenerateStockReport(object parameter)
        {
            try
            {
                var products = _context.Products
                    .Include(p => p.Categories)
                    .ToList();

                var stockBalances = _context.StockBalances
                    .GroupBy(sb => sb.ProductID)
                    .Select(g => new { ProductId = g.Key, TotalStock = g.Sum(sb => sb.Quantity) })
                    .ToDictionary(x => x.ProductId, x => x.TotalStock);

                StockReport = new ObservableCollection<StockReportItem>();

                foreach (var product in products)
                {
                    int currentStock = stockBalances.ContainsKey(product.ProductID) ? stockBalances[product.ProductID] : 0;
                    int minStock = product.MinStockLevel ?? 5;

                    string status = currentStock == 0 ? "Отсутствует" :
                                   currentStock < minStock ? "Ниже минимума" :
                                   currentStock < minStock * 2 ? "Близко к минимуму" : "Норма";

                    StockReport.Add(new StockReportItem
                    {
                        ProductId = product.ProductID,
                        ProductName = product.ProductName,
                        Category = product.Categories?.CategoryName ?? "Без категории",
                        CurrentStock = currentStock,
                        MinStockLevel = minStock,
                        Status = status,
                        UnitPrice = product.UnitPrice,
                        TotalValue = currentStock * product.UnitPrice
                    });
                }

                StatusMessage = $"Отчет по остаткам сгенерирован. Товаров: {StockReport.Count}";
                StatusColor = "#27ae60";
                AuditLogger.Log("REPORT_GENERATE", "StockReport", "Сформирован отчет по остаткам", metadata: $"Rows={StockReport.Count}");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка генерации отчета: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private bool CanExportSalesReport(object parameter)
        {
            return SalesReport != null && SalesReport.Count > 0;
        }

        private bool CanExportStockReport(object parameter)
        {
            return StockReport != null && StockReport.Count > 0;
        }

        private void ExportSalesReportPdf(object parameter)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = $"Отчет_по_продажам_{DateTime.Now:yyyyMMddHHmmss}.pdf",
                    DefaultExt = ".pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    CreateSalesReportPdf(dialog.FileName);
                    OnReportSaved(dialog.FileName, "Отчет по продажам", "PDF");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private void ExportStockReportPdf(object parameter)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = $"Отчет_по_остаткам_{DateTime.Now:yyyyMMddHHmmss}.pdf",
                    DefaultExt = ".pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    CreateStockReportPdf(dialog.FileName);
                    OnReportSaved(dialog.FileName, "Отчет по остаткам", "PDF");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private void ExportProductCatalogExcel(object parameter)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xls)|*.xls",
                    FileName = $"Каталог_товаров_{DateTime.Now:yyyyMMddHHmmss}.xls",
                    DefaultExt = ".xls"
                };

                if (dialog.ShowDialog() == true)
                {
                    CreateProductCatalogExcel(dialog.FileName);
                    OnReportSaved(dialog.FileName, "Каталог товаров", "Excel");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private void ExportSalesReportCsv(object parameter)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"Отчет_по_продажам_{DateTime.Now:yyyyMMddHHmmss}.csv",
                    DefaultExt = ".csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    CreateSalesReportCsv(dialog.FileName);
                    OnReportSaved(dialog.FileName, "Отчет по продажам", "CSV");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private void ExportSalesReportExcel(object parameter)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xls)|*.xls",
                    FileName = $"Отчет_по_продажам_{DateTime.Now:yyyyMMddHHmmss}.xls",
                    DefaultExt = ".xls"
                };

                if (dialog.ShowDialog() == true)
                {
                    CreateSalesReportExcel(dialog.FileName);
                    OnReportSaved(dialog.FileName, "Отчет по продажам", "Excel");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private void ExportStockReportCsv(object parameter)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"Отчет_по_остаткам_{DateTime.Now:yyyyMMddHHmmss}.csv",
                    DefaultExt = ".csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    CreateStockReportCsv(dialog.FileName);
                    OnReportSaved(dialog.FileName, "Отчет по остаткам", "CSV");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private void ExportStockReportExcel(object parameter)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xls)|*.xls",
                    FileName = $"Отчет_по_остаткам_{DateTime.Now:yyyyMMddHHmmss}.xls",
                    DefaultExt = ".xls"
                };

                if (dialog.ShowDialog() == true)
                {
                    CreateStockReportExcel(dialog.FileName);
                    OnReportSaved(dialog.FileName, "Отчет по остаткам", "Excel");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private void ExportProductCatalogPdf(object parameter)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = $"Каталог_товаров_{DateTime.Now:yyyyMMddHHmmss}.pdf",
                    DefaultExt = ".pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    CreateProductCatalogPdf(dialog.FileName);
                    OnReportSaved(dialog.FileName, "Каталог товаров", "PDF");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private void ExportProductCatalogCsv(object parameter)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"Каталог_товаров_{DateTime.Now:yyyyMMddHHmmss}.csv",
                    DefaultExt = ".csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    CreateProductCatalogCsv(dialog.FileName);
                    OnReportSaved(dialog.FileName, "Каталог товаров", "CSV");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения: {ex.Message}";
                StatusColor = "#e74c3c";
            }
        }

        private void CreateSalesReportPdf(string filename)
        {
            using (var document = new PdfDocument())
            {
                document.Info.Title = $"Отчет по продажам за {StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}";
                document.Info.Creator = "KPMurtazin";

                var page = document.AddPage();
                page.Width = XUnit.FromPoint(595); // A4 ширина
                page.Height = XUnit.FromPoint(842); // A4 высота

                // Не используем using для gfx, а создаем новую переменную при смене страницы
                var gfx = XGraphics.FromPdfPage(page);

                try
                {
                    var fontTitle = new XFont("Arial", 16, XFontStyleEx.Bold);
                    var fontHeader = new XFont("Arial", 10, XFontStyleEx.Bold);
                    var fontNormal = new XFont("Arial", 9, XFontStyleEx.Regular);

                    double yPos = 30;
                    double leftMargin = 40;

                    // Заголовок
                    gfx.DrawString("ОТЧЕТ ПО ПРОДАЖАМ", fontTitle, XBrushes.DarkBlue, leftMargin, yPos);
                    yPos += 25;
                    gfx.DrawString($"Период: {StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}", fontNormal, XBrushes.Black, leftMargin, yPos);
                    yPos += 25;
                    gfx.DrawString($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}", fontNormal, XBrushes.Black, leftMargin, yPos);
                    yPos += 30;

                    // Сводка
                    if (SalesSummary != null && SalesSummary.Count > 0)
                    {
                        gfx.DrawString("СВОДКА ПО СОТРУДНИКАМ", fontHeader, XBrushes.DarkBlue, leftMargin, yPos);
                        yPos += 20;

                        // Заголовки таблицы
                        gfx.DrawString("Сотрудник", fontHeader, XBrushes.Black, leftMargin, yPos);
                        gfx.DrawString("Продаж", fontHeader, XBrushes.Black, leftMargin + 200, yPos);
                        gfx.DrawString("Сумма", fontHeader, XBrushes.Black, leftMargin + 280, yPos);
                        gfx.DrawString("Ср. чек", fontHeader, XBrushes.Black, leftMargin + 360, yPos);
                        yPos += 15;

                        foreach (var item in SalesSummary)
                        {
                            gfx.DrawString(item.EmployeeName, fontNormal, XBrushes.Black, leftMargin, yPos);
                            gfx.DrawString(item.SalesCount.ToString(), fontNormal, XBrushes.Black, leftMargin + 200, yPos);
                            gfx.DrawString(item.TotalAmount.ToString("F2") + " ₽", fontNormal, XBrushes.Black, leftMargin + 280, yPos);
                            gfx.DrawString(item.AverageCheck.ToString("F2") + " ₽", fontNormal, XBrushes.Black, leftMargin + 360, yPos);
                            yPos += 15;
                        }
                        yPos += 20;
                    }

                    // Детали продаж
                    gfx.DrawString("ДЕТАЛИ ПРОДАЖ", fontHeader, XBrushes.DarkBlue, leftMargin, yPos);
                    yPos += 20;

                    // Заголовки деталей
                    gfx.DrawString("№", fontHeader, XBrushes.Black, leftMargin, yPos);
                    gfx.DrawString("Дата", fontHeader, XBrushes.Black, leftMargin + 40, yPos);
                    gfx.DrawString("Сотрудник", fontHeader, XBrushes.Black, leftMargin + 120, yPos);
                    gfx.DrawString("Способ оплаты", fontHeader, XBrushes.Black, leftMargin + 250, yPos);
                    gfx.DrawString("Кол-во", fontHeader, XBrushes.Black, leftMargin + 350, yPos);
                    gfx.DrawString("Сумма", fontHeader, XBrushes.Black, leftMargin + 400, yPos);
                    yPos += 15;

                    int count = 0;
                    foreach (var item in SalesReport.Take(50)) // Ограничим для примера
                    {
                        if (yPos > page.Height.Point - 50)
                        {
                            // Новая страница
                            page = document.AddPage();
                            page.Width = XUnit.FromPoint(595);
                            page.Height = XUnit.FromPoint(842);

                            // Освобождаем предыдущий gfx и создаем новый
                            gfx.Dispose();
                            gfx = XGraphics.FromPdfPage(page);

                            yPos = 30;

                            // Повторяем заголовки на новой странице
                            gfx.DrawString("№", fontHeader, XBrushes.Black, leftMargin, yPos);
                            gfx.DrawString("Дата", fontHeader, XBrushes.Black, leftMargin + 40, yPos);
                            gfx.DrawString("Сотрудник", fontHeader, XBrushes.Black, leftMargin + 120, yPos);
                            gfx.DrawString("Способ оплаты", fontHeader, XBrushes.Black, leftMargin + 250, yPos);
                            gfx.DrawString("Кол-во", fontHeader, XBrushes.Black, leftMargin + 350, yPos);
                            gfx.DrawString("Сумма", fontHeader, XBrushes.Black, leftMargin + 400, yPos);
                            yPos += 15;
                        }

                        gfx.DrawString(item.SaleId.ToString(), fontNormal, XBrushes.Black, leftMargin, yPos);
                        gfx.DrawString(item.SaleDate.ToString("dd.MM.yy HH:mm"), fontNormal, XBrushes.Black, leftMargin + 40, yPos);
                        gfx.DrawString(item.EmployeeName, fontNormal, XBrushes.Black, leftMargin + 120, yPos);
                        gfx.DrawString(item.PaymentMethod, fontNormal, XBrushes.Black, leftMargin + 250, yPos);
                        gfx.DrawString(item.ItemsCount.ToString(), fontNormal, XBrushes.Black, leftMargin + 350, yPos);
                        gfx.DrawString(item.TotalAmount.ToString("F2") + " ₽", fontNormal, XBrushes.Black, leftMargin + 400, yPos);
                        yPos += 15;
                        count++;
                    }

                    if (SalesReport.Count > 50)
                    {
                        yPos += 10;
                        gfx.DrawString($"... и еще {SalesReport.Count - 50} записей", fontNormal, XBrushes.Gray, leftMargin, yPos);
                    }

                    // Итог
                    yPos = page.Height.Point - 50;
                    gfx.DrawString("────────────────────────────────────", fontNormal, XBrushes.Black, leftMargin, yPos);
                    yPos += 15;
                    gfx.DrawString($"ОБЩИЙ ИТОГ: {SalesReport.Sum(s => s.TotalAmount):F2} ₽", fontHeader, XBrushes.DarkBlue, leftMargin, yPos);
                }
                finally
                {
                    gfx.Dispose();
                }

                document.Save(filename);
            }
        }

        private void CreateStockReportPdf(string filename)
        {
            using (var document = new PdfDocument())
            {
                document.Info.Title = "Отчет по остаткам товаров";
                document.Info.Creator = "KPMurtazin";

                var page = document.AddPage();
                page.Width = XUnit.FromPoint(595);
                page.Height = XUnit.FromPoint(842);

                var gfx = XGraphics.FromPdfPage(page);

                try
                {
                    var fontTitle = new XFont("Arial", 16, XFontStyleEx.Bold);
                    var fontHeader = new XFont("Arial", 10, XFontStyleEx.Bold);
                    var fontNormal = new XFont("Arial", 9, XFontStyleEx.Regular);

                    double yPos = 30;
                    double leftMargin = 40;

                    // Заголовок
                    gfx.DrawString("ОТЧЕТ ПО ОСТАТКАМ ТОВАРОВ", fontTitle, XBrushes.DarkBlue, leftMargin, yPos);
                    yPos += 25;
                    gfx.DrawString($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}", fontNormal, XBrushes.Black, leftMargin, yPos);
                    yPos += 30;

                    // Сводка
                    int totalProducts = StockReport.Count;
                    int outOfStock = StockReport.Count(s => s.CurrentStock == 0);
                    int belowMin = StockReport.Count(s => s.Status == "Ниже минимума");
                    decimal totalValue = StockReport.Sum(s => s.TotalValue);

                    gfx.DrawString("СВОДКА", fontHeader, XBrushes.DarkBlue, leftMargin, yPos);
                    yPos += 20;
                    gfx.DrawString($"Всего товаров: {totalProducts}", fontNormal, XBrushes.Black, leftMargin, yPos);
                    yPos += 15;
                    gfx.DrawString($"Отсутствуют: {outOfStock}", fontNormal, XBrushes.Red, leftMargin, yPos);
                    yPos += 15;
                    gfx.DrawString($"Ниже минимума: {belowMin}", fontNormal, XBrushes.Orange, leftMargin, yPos);
                    yPos += 15;
                    gfx.DrawString($"Общая стоимость: {totalValue:F2} ₽", fontNormal, XBrushes.Black, leftMargin, yPos);
                    yPos += 25;

                    // Таблица остатков
                    gfx.DrawString("ДЕТАЛИ", fontHeader, XBrushes.DarkBlue, leftMargin, yPos);
                    yPos += 20;

                    // Заголовки
                    gfx.DrawString("Товар", fontHeader, XBrushes.Black, leftMargin, yPos);
                    gfx.DrawString("Категория", fontHeader, XBrushes.Black, leftMargin + 180, yPos);
                    gfx.DrawString("Остаток", fontHeader, XBrushes.Black, leftMargin + 280, yPos);
                    gfx.DrawString("Мин.", fontHeader, XBrushes.Black, leftMargin + 340, yPos);
                    gfx.DrawString("Статус", fontHeader, XBrushes.Black, leftMargin + 380, yPos);
                    gfx.DrawString("Стоимость", fontHeader, XBrushes.Black, leftMargin + 440, yPos);
                    yPos += 15;

                    foreach (var item in StockReport.OrderBy(s => s.Status).ThenBy(s => s.ProductName))
                    {
                        if (yPos > page.Height.Point - 50)
                        {
                            page = document.AddPage();
                            page.Width = XUnit.FromPoint(595);
                            page.Height = XUnit.FromPoint(842);

                            gfx.Dispose();
                            gfx = XGraphics.FromPdfPage(page);

                            yPos = 30;

                            // Повторяем заголовки
                            gfx.DrawString("Товар", fontHeader, XBrushes.Black, leftMargin, yPos);
                            gfx.DrawString("Категория", fontHeader, XBrushes.Black, leftMargin + 180, yPos);
                            gfx.DrawString("Остаток", fontHeader, XBrushes.Black, leftMargin + 280, yPos);
                            gfx.DrawString("Мин.", fontHeader, XBrushes.Black, leftMargin + 340, yPos);
                            gfx.DrawString("Статус", fontHeader, XBrushes.Black, leftMargin + 380, yPos);
                            gfx.DrawString("Стоимость", fontHeader, XBrushes.Black, leftMargin + 440, yPos);
                            yPos += 15;
                        }

                        XBrush statusBrush = item.Status == "Норма" ? XBrushes.Green :
                                            item.Status == "Ниже минимума" ? XBrushes.Red :
                                            item.Status == "Близко к минимуму" ? XBrushes.Orange : XBrushes.Gray;

                        gfx.DrawString(TruncateString(item.ProductName, 25), fontNormal, XBrushes.Black, leftMargin, yPos);
                        gfx.DrawString(TruncateString(item.Category, 15), fontNormal, XBrushes.Black, leftMargin + 180, yPos);
                        gfx.DrawString(item.CurrentStock.ToString(), fontNormal, XBrushes.Black, leftMargin + 280, yPos);
                        gfx.DrawString(item.MinStockLevel.ToString(), fontNormal, XBrushes.Black, leftMargin + 340, yPos);
                        gfx.DrawString(item.Status, fontNormal, statusBrush, leftMargin + 380, yPos);
                        gfx.DrawString(item.TotalValue.ToString("F2") + " ₽", fontNormal, XBrushes.Black, leftMargin + 440, yPos);
                        yPos += 15;
                    }
                }
                finally
                {
                    gfx.Dispose();
                }

                document.Save(filename);
            }
        }

        private void CreateProductCatalogCsv(string filename)
        {
            var products = _context.Products
                .Include(p => p.Categories)
                .ToList();

            var stockBalances = _context.StockBalances
                .GroupBy(sb => sb.ProductID)
                .Select(g => new { ProductId = g.Key, TotalStock = g.Sum(sb => sb.Quantity) })
                .ToDictionary(x => x.ProductId, x => x.TotalStock);

            using (var writer = new StreamWriter(filename, false, System.Text.Encoding.UTF8))
            {
                // Заголовки
                writer.WriteLine("ID;Наименование;Категория;Штрих-код;Производитель;Модель;Цена;Гарантия (мес);Остаток");

                // Данные
                foreach (var product in products.OrderBy(p => p.ProductName))
                {
                    int stock = stockBalances.ContainsKey(product.ProductID) ? stockBalances[product.ProductID] : 0;

                    writer.WriteLine($"{product.ProductID};" +
                                    $"{EscapeCsv(product.ProductName)};" +
                                    $"{EscapeCsv(product.Categories?.CategoryName ?? "")};" +
                                    $"{product.Barcode};" +
                                    $"{EscapeCsv(product.Manufacturer ?? "")};" +
                                    $"{EscapeCsv(product.Model ?? "")};" +
                                    $"{product.UnitPrice:F2};" +
                                    $"{product.WarrantyMonths};" +
                                    $"{stock}");
                }
            }
        }

        private void CreateSalesReportCsv(string filename)
        {
            using (var writer = new StreamWriter(filename, false, System.Text.Encoding.UTF8))
            {
                writer.WriteLine("ID продажи;Дата;Сотрудник;Способ оплаты;Позиций;Сумма");
                foreach (var item in SalesReport.OrderByDescending(s => s.SaleDate))
                {
                    writer.WriteLine($"{item.SaleId};{item.SaleDate:dd.MM.yyyy HH:mm};{EscapeCsv(item.EmployeeName)};{EscapeCsv(item.PaymentMethod)};{item.ItemsCount};{item.TotalAmount:F2}");
                }
            }
        }

        private void CreateStockReportCsv(string filename)
        {
            using (var writer = new StreamWriter(filename, false, System.Text.Encoding.UTF8))
            {
                writer.WriteLine("ID товара;Наименование;Категория;Остаток;Мин. остаток;Статус;Цена;Стоимость");
                foreach (var item in StockReport.OrderBy(s => s.Status).ThenBy(s => s.ProductName))
                {
                    writer.WriteLine($"{item.ProductId};{EscapeCsv(item.ProductName)};{EscapeCsv(item.Category)};{item.CurrentStock};{item.MinStockLevel};{EscapeCsv(item.Status)};{item.UnitPrice:F2};{item.TotalValue:F2}");
                }
            }
        }

        private void CreateSalesReportExcel(string filename)
        {
            using (var writer = new StreamWriter(filename, false, System.Text.Encoding.Unicode))
            {
                writer.WriteLine("ID продажи\tДата\tСотрудник\tСпособ оплаты\tПозиций\tСумма");
                foreach (var item in SalesReport.OrderByDescending(s => s.SaleDate))
                {
                    writer.WriteLine($"{item.SaleId}\t{item.SaleDate:dd.MM.yyyy HH:mm}\t{item.EmployeeName}\t{item.PaymentMethod}\t{item.ItemsCount}\t{item.TotalAmount:F2}");
                }
            }
        }

        private void CreateStockReportExcel(string filename)
        {
            using (var writer = new StreamWriter(filename, false, System.Text.Encoding.Unicode))
            {
                writer.WriteLine("ID товара\tНаименование\tКатегория\tОстаток\tМин. остаток\tСтатус\tЦена\tСтоимость");
                foreach (var item in StockReport.OrderBy(s => s.Status).ThenBy(s => s.ProductName))
                {
                    writer.WriteLine($"{item.ProductId}\t{item.ProductName}\t{item.Category}\t{item.CurrentStock}\t{item.MinStockLevel}\t{item.Status}\t{item.UnitPrice:F2}\t{item.TotalValue:F2}");
                }
            }
        }

        private void CreateProductCatalogExcel(string filename)
        {
            var products = _context.Products
                .Include(p => p.Categories)
                .ToList();

            var stockBalances = _context.StockBalances
                .GroupBy(sb => sb.ProductID)
                .Select(g => new { ProductId = g.Key, TotalStock = g.Sum(sb => sb.Quantity) })
                .ToDictionary(x => x.ProductId, x => x.TotalStock);

            using (var writer = new StreamWriter(filename, false, System.Text.Encoding.Unicode))
            {
                writer.WriteLine("ID\tНаименование\tКатегория\tШтрих-код\tПроизводитель\tМодель\tЦена\tГарантия (мес)\tОстаток");
                foreach (var product in products.OrderBy(p => p.ProductName))
                {
                    int stock = stockBalances.ContainsKey(product.ProductID) ? stockBalances[product.ProductID] : 0;
                    writer.WriteLine($"{product.ProductID}\t{product.ProductName}\t{product.Categories?.CategoryName ?? ""}\t{product.Barcode}\t{product.Manufacturer ?? ""}\t{product.Model ?? ""}\t{product.UnitPrice:F2}\t{product.WarrantyMonths}\t{stock}");
                }
            }
        }

        private void CreateProductCatalogPdf(string filename)
        {
            var products = _context.Products
                .Include(p => p.Categories)
                .OrderBy(p => p.ProductName)
                .ToList();

            using (var document = new PdfDocument())
            {
                document.Info.Title = "Каталог товаров";
                var page = document.AddPage();
                page.Width = XUnit.FromPoint(595);
                page.Height = XUnit.FromPoint(842);
                var gfx = XGraphics.FromPdfPage(page);
                var fontTitle = new XFont("Arial", 14, XFontStyleEx.Bold);
                var font = new XFont("Arial", 9, XFontStyleEx.Regular);
                double y = 30;
                const double left = 30;

                gfx.DrawString("КАТАЛОГ ТОВАРОВ", fontTitle, XBrushes.DarkBlue, left, y);
                y += 25;
                gfx.DrawString("Наименование", font, XBrushes.Black, left, y);
                gfx.DrawString("Категория", font, XBrushes.Black, left + 220, y);
                gfx.DrawString("Цена", font, XBrushes.Black, left + 360, y);
                y += 15;

                foreach (var product in products)
                {
                    if (y > page.Height.Point - 40)
                    {
                        page = document.AddPage();
                        page.Width = XUnit.FromPoint(595);
                        page.Height = XUnit.FromPoint(842);
                        gfx.Dispose();
                        gfx = XGraphics.FromPdfPage(page);
                        y = 30;
                    }

                    gfx.DrawString(TruncateString(product.ProductName, 40), font, XBrushes.Black, left, y);
                    gfx.DrawString(TruncateString(product.Categories?.CategoryName ?? "", 20), font, XBrushes.Black, left + 220, y);
                    gfx.DrawString($"{product.UnitPrice:F2} ₽", font, XBrushes.Black, left + 360, y);
                    y += 14;
                }

                gfx.Dispose();
                document.Save(filename);
            }
        }

        private void OnReportSaved(string fileName, string reportName, string format)
        {
            StatusMessage = $"{reportName} сохранен: {Path.GetFileName(fileName)}";
            StatusColor = "#27ae60";
            AuditLogger.Log("REPORT_EXPORT", reportName, $"{reportName} экспортирован в формат {format}", metadata: $"File={Path.GetFileName(fileName)}");

            var result = MessageBox.Show("Файл сохранен. Открыть его?",
                "Успешно", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Process.Start(fileName);
            }
        }

        private string TruncateString(string s, int maxLength)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Length <= maxLength ? s : s.Substring(0, maxLength - 3) + "...";
        }

        private string EscapeCsv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains(";") || s.Contains("\"") || s.Contains("\n"))
            {
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }
            return s;
        }

        public void DisposeContext()
        {
            _context?.Dispose();
        }
    }
}