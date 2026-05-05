using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace DiplomMurtazin.Core
{
    public class Torg12ExcelData
    {
        public string DocumentNumber { get; set; }
        public DateTime DocumentDate { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverAddress { get; set; }
        public string Basis { get; set; }
        public List<Torg12ExcelRow> Rows { get; set; } = new List<Torg12ExcelRow>();
    }

    public class Torg12ExcelRow
    {
        public string ProductName { get; set; }
        public string Barcode { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public static class Torg12ExcelInteropService
    {
        public static bool IsExcelInstalled()
        {
            return Type.GetTypeFromProgID("Excel.Application") != null;
        }

        public static void ExportFromTemplate(
            string templatePath,
            string outputPath,
            string docNumber,
            DateTime docDate,
            string receiverName,
            string receiverAddress,
            string basis,
            IEnumerable<Torg12ExcelRow> rows)
        {
            if (!File.Exists(templatePath))
                throw new FileNotFoundException("Не найден шаблон ТОРГ-12", templatePath);

            if (!IsExcelInstalled())
                throw new InvalidOperationException("Microsoft Excel не установлен. Нужен для экспорта ТОРГ-12 по шаблону.");

            File.Copy(templatePath, outputPath, true);

            dynamic excel = null;
            dynamic wb = null;
            dynamic ws = null;

            try
            {
                excel = Activator.CreateInstance(Type.GetTypeFromProgID("Excel.Application"));
                excel.Visible = false;
                excel.DisplayAlerts = false;

                wb = excel.Workbooks.Open(outputPath);
                ws = wb.Worksheets[1];

                // Find header cells by label search (robust across template variants)
                SetNearLabel(ws, new[] { "Номер", "№" }, docNumber);
                SetNearLabel(ws, new[] { "Дата" }, docDate.ToString("dd.MM.yyyy"));
                SetNearLabel(ws, new[] { "Кому отпустить", "Грузополучатель", "Получатель" }, receiverName);
                if (!string.IsNullOrWhiteSpace(receiverAddress))
                    SetNearLabel(ws, new[] { "Адрес", "Адрес получателя" }, receiverAddress);
                if (!string.IsNullOrWhiteSpace(basis))
                    SetNearLabel(ws, new[] { "Основание", "Основание отпуска" }, basis);

                // Locate table header row
                var tableHeader = FindFirstCellContains(ws, new[] { "Наименование", "Наименование товара" });
                if (!tableHeader.HasValue)
                    throw new InvalidOperationException("Не удалось найти таблицу позиций в шаблоне ТОРГ-12 (заголовок 'Наименование').");

                int headerRow = tableHeader.Value.Row;
                int nameCol = tableHeader.Value.Column;

                // Try to locate other columns near header
                int qtyCol = FindInRow(ws, headerRow, new[] { "Кол", "Количество" }) ?? (nameCol + 1);
                int priceCol = FindInRow(ws, headerRow, new[] { "Цена" }) ?? (qtyCol + 1);
                int sumCol = FindInRow(ws, headerRow, new[] { "Сумма", "Стоимость" }) ?? (priceCol + 1);
                int? barcodeCol = FindInRow(ws, headerRow, new[] { "Штрих", "Код", "Артикул" });

                int startRow = headerRow + 1;
                int i = 0;
                foreach (var r in rows)
                {
                    int rowIndex = startRow + i;
                    ws.Cells[rowIndex, nameCol].Value = r.ProductName;
                    if (barcodeCol.HasValue)
                        ws.Cells[rowIndex, barcodeCol.Value].Value = r.Barcode;
                    ws.Cells[rowIndex, qtyCol].Value = r.Quantity;
                    ws.Cells[rowIndex, priceCol].Value = (double)r.UnitPrice;
                    ws.Cells[rowIndex, sumCol].Value = (double)(r.UnitPrice * r.Quantity);
                    i++;
                }

                wb.Save();
                wb.Close(true);
            }
            finally
            {
                try { if (wb != null) Marshal.FinalReleaseComObject(wb); } catch { }
                try { if (ws != null) Marshal.FinalReleaseComObject(ws); } catch { }
                try
                {
                    if (excel != null)
                    {
                        excel.Quit();
                        Marshal.FinalReleaseComObject(excel);
                    }
                }
                catch { }
            }
        }

        public static Torg12ExcelData Import(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Файл ТОРГ-12 не найден", filePath);

            if (!IsExcelInstalled())
                throw new InvalidOperationException("Microsoft Excel не установлен. Нужен для импорта ТОРГ-12 из .xls.");

            dynamic excel = null;
            dynamic wb = null;
            dynamic ws = null;

            try
            {
                excel = Activator.CreateInstance(Type.GetTypeFromProgID("Excel.Application"));
                excel.Visible = false;
                excel.DisplayAlerts = false;

                wb = excel.Workbooks.Open(filePath, ReadOnly: true);
                ws = wb.Worksheets[1];

                var result = new Torg12ExcelData
                {
                    DocumentDate = DateTime.Today
                };

                result.DocumentNumber = GetNearLabel(ws, new[] { "Номер", "№" });
                var dateStr = GetNearLabel(ws, new[] { "Дата" });
                if (DateTime.TryParse(dateStr, out DateTime parsedDate))
                    result.DocumentDate = parsedDate;

                result.ReceiverName = GetNearLabel(ws, new[] { "Кому отпустить", "Грузополучатель", "Получатель" });
                result.ReceiverAddress = GetNearLabel(ws, new[] { "Адрес", "Адрес получателя" });
                result.Basis = GetNearLabel(ws, new[] { "Основание", "Основание отпуска" });

                var tableHeader = FindFirstCellContains(ws, new[] { "Наименование", "Наименование товара" });
                if (!tableHeader.HasValue)
                    throw new InvalidOperationException("Не удалось найти таблицу позиций (заголовок 'Наименование').");

                int headerRow = tableHeader.Value.Row;
                int nameCol = tableHeader.Value.Column;
                int qtyCol = FindInRow(ws, headerRow, new[] { "Кол", "Количество" }) ?? (nameCol + 1);
                int priceCol = FindInRow(ws, headerRow, new[] { "Цена" }) ?? (qtyCol + 1);
                int? barcodeCol = FindInRow(ws, headerRow, new[] { "Штрих", "Код", "Артикул" });

                int row = headerRow + 1;
                for (int safety = 0; safety < 500; safety++)
                {
                    string name = Convert.ToString(ws.Cells[row, nameCol].Value)?.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                        break;

                    string barcode = barcodeCol.HasValue ? Convert.ToString(ws.Cells[row, barcodeCol.Value].Value)?.Trim() : null;
                    int qty = TryInt(ws.Cells[row, qtyCol].Value);
                    decimal price = TryDecimal(ws.Cells[row, priceCol].Value);

                    if (qty <= 0)
                    {
                        row++;
                        continue;
                    }

                    result.Rows.Add(new Torg12ExcelRow
                    {
                        ProductName = name,
                        Barcode = barcode,
                        Quantity = qty,
                        UnitPrice = price
                    });

                    row++;
                }

                return result;
            }
            finally
            {
                try { if (wb != null) { wb.Close(false); Marshal.FinalReleaseComObject(wb); } } catch { }
                try { if (ws != null) Marshal.FinalReleaseComObject(ws); } catch { }
                try
                {
                    if (excel != null)
                    {
                        excel.Quit();
                        Marshal.FinalReleaseComObject(excel);
                    }
                }
                catch { }
            }
        }

        private static int TryInt(object v)
        {
            if (v is null) return 0;
            if (v is int i) return i;
            if (v is double d) return (int)Math.Round(d);
            if (int.TryParse(Convert.ToString(v), out var r)) return r;
            return 0;
        }

        private static decimal TryDecimal(object v)
        {
            if (v is null) return 0m;
            if (v is decimal m) return m;
            if (v is double d) return (decimal)d;
            var s = Convert.ToString(v)?.Replace(',', '.');
            if (decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var r))
                return r;
            return 0m;
        }

        private static void SetNearLabel(dynamic ws, string[] labelVariants, string value)
        {
            var labelCell = FindFirstCellContains(ws, labelVariants);
            if (!labelCell.HasValue) return;
            // Try right cell first, else next row same col
            ws.Cells[labelCell.Row, labelCell.Column + 1].Value = value;
        }

        private static string GetNearLabel(dynamic ws, string[] labelVariants)
        {
            var labelCell = FindFirstCellContains(ws, labelVariants);
            if (!labelCell.HasValue) return null;
            var right = ws.Cells[labelCell.Row, labelCell.Column + 1].Value;
            if (right != null && !(right is DBNull)) return Convert.ToString(right);
            var down = ws.Cells[labelCell.Row + 1, labelCell.Column].Value;
            return down != null ? Convert.ToString(down) : null;
        }

        private static (int Row, int Column)? FindFirstCellContains(dynamic ws, string[] variants)
        {
            dynamic used = ws.UsedRange;
            int rows = used.Rows.Count;
            int cols = used.Columns.Count;

            for (int r = 1; r <= rows; r++)
            {
                for (int c = 1; c <= cols; c++)
                {
                    var v = used.Cells[r, c].Value;
                    if (v is null || v is DBNull) continue;
                    string s = Convert.ToString(v);
                    if (string.IsNullOrWhiteSpace(s)) continue;
                    foreach (var varnt in variants)
                    {
                        if (s.IndexOf(varnt, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return (used.Row + r - 1, used.Column + c - 1);
                        }
                    }
                }
            }
            return null;
        }

        private static int? FindInRow(dynamic ws, int row, string[] variants)
        {
            dynamic used = ws.UsedRange;
            int cols = used.Columns.Count;
            for (int c = 1; c <= cols; c++)
            {
                var v = ws.Cells[row, c].Value;
                if (v is null) continue;
                string s = Convert.ToString(v);
                if (string.IsNullOrWhiteSpace(s)) continue;
                foreach (var varnt in variants)
                {
                    if (s.IndexOf(varnt, StringComparison.OrdinalIgnoreCase) >= 0)
                        return c;
                }
            }
            return null;
        }
    }
}

