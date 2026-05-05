using DiplomMurtazin.Model;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Windows;

namespace DiplomMurtazin.View
{
    public partial class ReceiptWindow : Window
    {
        private ReceiptModel _receipt;

        public ReceiptWindow(ReceiptModel receipt)
        {
            InitializeComponent();
            _receipt = receipt;
            DataContext = receipt;
        }

        private void PrintPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = $"Чек_{_receipt.SaleNumber}_{DateTime.Now:yyyyMMddHHmmss}.pdf",
                    DefaultExt = ".pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    CreatePdf(dialog.FileName);

                    MessageBox.Show($"Чек сохранен в PDF: {dialog.FileName}",
                                  "Успешно",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения PDF: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void CreatePdf(string filename)
        {
            using (var document = new PdfDocument())
            {
                document.Info.Title = $"Кассовый чек №{_receipt.SaleNumber}";
                document.Info.Creator = "KPMurtazin";

                var page = document.AddPage();
                page.Width = XUnit.FromPoint(250);
                page.Height = XUnit.FromPoint(800);

                using (var gfx = XGraphics.FromPdfPage(page))
                {
                    var font = new XFont("Courier New", 8, XFontStyleEx.Regular);
                    var fontBold = new XFont("Courier New", 8, XFontStyleEx.Bold);

                    double yPos = 20;
                    double leftMargin = 15;
                    double rightMargin = page.Width.Point - 15;

                    // Шапка
                    yPos = DrawText(gfx, "────────────────────────────────", font, leftMargin, yPos);
                    yPos = DrawText(gfx, "КАССОВЫЙ ЧЕК", fontBold, leftMargin, yPos, true);
                    yPos = DrawText(gfx, $"Продажа №{_receipt.SaleNumber} Смена №{_receipt.ShiftNumber}", font, leftMargin, yPos);
                    yPos = DrawText(gfx, _receipt.DateTime.ToString("dd.MM.yy HH:mm"), font, leftMargin, yPos);
                    yPos = DrawText(gfx, "────────────────────────────────", font, leftMargin, yPos);

                    // Товары
                    foreach (var item in _receipt.Items)
                    {
                        yPos = DrawText(gfx, item.Name, fontBold, leftMargin, yPos);
                        yPos = DrawLine(gfx, $"{item.Quantity:F3} X {item.Price:F2} = {item.Total:F2}", font, leftMargin, yPos);
                    }

                    // Итоги
                    yPos = DrawText(gfx, "────────────────────────────────", font, leftMargin, yPos);
                    yPos = DrawTwoColumn(gfx, "ИТОГ =", _receipt.TotalAmount.ToString("F2"), fontBold, leftMargin, rightMargin, yPos);
                    yPos = DrawTwoColumn(gfx, "Сумма БЕЗ НДС =", _receipt.AmountWithoutVat.ToString("F2"), font, leftMargin, rightMargin, yPos);
                    yPos = DrawTwoColumn(gfx, "НАЛИЧНЫМИ =", _receipt.CashPayment.ToString("F2"), font, leftMargin, rightMargin, yPos);
                    yPos = DrawText(gfx, "────────────────────────────────", font, leftMargin, yPos);

                    // Информация о компании
                    yPos = DrawText(gfx, _receipt.CompanyName, fontBold, leftMargin, yPos);
                    yPos = DrawText(gfx, _receipt.Address, font, leftMargin, yPos, true, 8);
                    yPos = DrawText(gfx, $"КАССИР {_receipt.Cashier}", fontBold, leftMargin, yPos);
                    yPos = DrawText(gfx, $"МЕСТО РАСЧЕТОВ: {_receipt.Place}", font, leftMargin, yPos);
                    yPos = DrawText(gfx, "────────────────────────────────", font, leftMargin, yPos);

                    // Фискальные данные
                    yPos = DrawTwoColumn(gfx, "Сайт ФНС:", _receipt.Website, font, leftMargin, rightMargin, yPos);
                    yPos = DrawTwoColumn(gfx, "РН ККТ:", _receipt.RnKkt, font, leftMargin, rightMargin, yPos);
                    yPos = DrawTwoColumn(gfx, "ЗН ККТ:", _receipt.ZnKkt, font, leftMargin, rightMargin, yPos);
                    yPos = DrawTwoColumn(gfx, "ФН N", _receipt.FnNumber, font, leftMargin, rightMargin, yPos);
                    yPos = DrawTwoColumn(gfx, "СНО:", _receipt.Sno, font, leftMargin, rightMargin, yPos);
                    yPos = DrawTwoColumn(gfx, "ФД №", $"000000{_receipt.FdNumber}", font, leftMargin, rightMargin, yPos);
                    yPos = DrawTwoColumn(gfx, "ФП:", _receipt.Fp, font, leftMargin, rightMargin, yPos);
                    yPos = DrawTwoColumn(gfx, "ИНН:", _receipt.Inn, font, leftMargin, rightMargin, yPos);
                    yPos = DrawTwoColumn(gfx, "Смена №", $"00{_receipt.ShiftNumber}", font, leftMargin, rightMargin, yPos);
                    yPos = DrawTwoColumn(gfx, "Чек №", $"00000{_receipt.DocumentNumber}", font, leftMargin, rightMargin, yPos);

                    yPos = DrawText(gfx, "ПРИХОД", fontBold, leftMargin, yPos, true);
                    yPos = DrawText(gfx, _receipt.DateTime.ToString("dd.MM.yy HH:mm"), font, leftMargin, yPos);
                }

                document.Save(filename);
            }
        }

        private double DrawText(XGraphics gfx, string text, XFont font, double x, double y, bool center = false, double fontSize = 8)
        {
            if (center)
            {
                var size = gfx.MeasureString(text, font);
                x = (250 - size.Width) / 2;
            }
            gfx.DrawString(text, font, XBrushes.Black, x, y + 12);
            return y + 12;
        }

        private double DrawLine(XGraphics gfx, string text, XFont font, double x, double y)
        {
            gfx.DrawString(text, font, XBrushes.Black, x, y + 12);
            return y + 12;
        }

        private double DrawTwoColumn(XGraphics gfx, string left, string right, XFont font, double leftMargin, double rightMargin, double y)
        {
            gfx.DrawString(left, font, XBrushes.Black, leftMargin, y + 12);

            var rightSize = gfx.MeasureString(right, font);
            gfx.DrawString(right, font, XBrushes.Black, rightMargin - rightSize.Width, y + 12);

            return y + 12;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}