using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Excel = Microsoft.Office.Interop.Excel;

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

        public static Torg12ExcelData Import(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Файл не найден", filePath);

            Excel.Application excel = null;
            Excel.Workbook wb = null;
            Excel.Worksheet ws = null;

            try
            {
                excel = new Excel.Application();
                wb = excel.Workbooks.Open(filePath, ReadOnly: true);
                ws = (Excel.Worksheet)wb.Worksheets[1];

                var result = new Torg12ExcelData
                {
                    DocumentDate = DateTime.Today
                };

                var used = ws.UsedRange;

                int rows = used.Rows.Count;
                int cols = used.Columns.Count;

                int headerRow = -1;
                int nameCol = -1;

                // ищем "Наименование"
                for (int r = 1; r <= rows; r++)
                {
                    for (int c = 1; c <= cols; c++)
                    {
                        var val = GetCell(used, r, c);
                        if (val == null) continue;

                        string s = val.ToString();

                        if (s.IndexOf("Наименование", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            headerRow = r;
                            nameCol = c;
                            break;
                        }
                    }
                    if (headerRow != -1) break;
                }

                if (headerRow == -1)
                    throw new Exception("Не найдена таблица (Наименование)");

                int qtyCol = nameCol + 1;
                int priceCol = nameCol + 2;

                int row = headerRow + 1;

                while (true)
                {
                    var nameVal = GetCell(ws, row, nameCol);
                    if (nameVal == null) break;

                    string name = nameVal.ToString().Trim();
                    if (string.IsNullOrWhiteSpace(name)) break;

                    int qty = ToInt(GetCell(ws, row, qtyCol));
                    decimal price = ToDecimal(GetCell(ws, row, priceCol));

                    if (qty > 0)
                    {
                        result.Rows.Add(new Torg12ExcelRow
                        {
                            ProductName = name,
                            Quantity = qty,
                            UnitPrice = price
                        });
                    }

                    row++;
                }

                return result;
            }
            catch (Exception ex)
            {
                File.WriteAllText("excel_error.log", ex.ToString());
                throw;
            }
            finally
            {
                if (wb != null) wb.Close(false);
                if (excel != null) excel.Quit();

                Release(ws);
                Release(wb);
                Release(excel);
            }
        }

        // ================= helpers =================

        private static object GetCell(object sheet, int r, int c)
        {
            try
            {
                if (sheet is Excel.Range range)
                    return range.Cells[r, c].Value2;

                if (sheet is Excel.Worksheet ws)
                    return ((Excel.Range)ws.Cells[r, c]).Value2;

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static int ToInt(object v)
        {
            if (v == null) return 0;
            if (v is double d) return (int)d;
            if (int.TryParse(v.ToString(), out var r)) return r;
            return 0;
        }

        private static decimal ToDecimal(object v)
        {
            if (v == null) return 0;
            if (v is double d) return (decimal)d;

            var s = v.ToString().Replace(',', '.');
            if (decimal.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var r))
                return r;

            return 0;
        }
        public static void ExportFromTemplate(
    string templatePath,
    string outputPath,
    string documentNumber,
    DateTime documentDate,
    string receiverName,
    string receiverAddress,
    string basis,
    List<Torg12ExcelRow> rows)
        {
            if (!File.Exists(templatePath))
                throw new FileNotFoundException("Шаблон Excel не найден", templatePath);

            Excel.Application excel = null;
            Excel.Workbook wb = null;
            Excel.Worksheet ws = null;

            try
            {
                excel = new Excel.Application();
                excel.Visible = false;

                wb = excel.Workbooks.Open(templatePath);
                ws = (Excel.Worksheet)wb.Worksheets[1];

                // ====== ШАПКА (подстрой под свой шаблон если нужно) ======
                ((Excel.Range)ws.Cells[26, 50]).Value2 = documentNumber;
                ((Excel.Range)ws.Cells[26, 63]).Value2 = documentDate.ToShortDateString();
                ((Excel.Range)ws.Cells[12, 12]).Value2 = receiverName + "   " + receiverAddress;
                ((Excel.Range)ws.Cells[18, 9]).Value2 = basis;

                int startRow = 31;
                for (int i = 0; i < rows.Count; i++)
                {
                    var r = rows[i];
                    int row = startRow + i;

                    // Явное приведение каждой ячейки:
                    ((Excel.Range)ws.Cells[row, 1]).Value2 = i + 1;
                    ((Excel.Range)ws.Cells[row, 4]).Value2 = r.ProductName;
                    ((Excel.Range)ws.Cells[row, 20]).Value2 = r.Barcode;
                    ((Excel.Range)ws.Cells[row, 39]).Value2 = r.Quantity;
                    ((Excel.Range)ws.Cells[row, 60]).Value2 = r.UnitPrice;
                    ((Excel.Range)ws.Cells[row, 89]).Value2 = r.Quantity * r.UnitPrice;
                }

                wb.SaveAs(outputPath);
            }
            finally
            {
                if (wb != null) wb.Close();
                if (excel != null) excel.Quit();

                Release(ws);
                Release(wb);
                Release(excel);
            }
        }
        private static void Release(object obj)
        {
            try
            {
                if (obj != null)
                    Marshal.ReleaseComObject(obj);
            }
            catch { }
        }
    }
}