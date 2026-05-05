using DiplomMurtazin.Model;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace DiplomMurtazin.ViewModel
{
    public class ReceiptViewModel : BaseViewModel
    {
        private int _receiptNumber;
        private DateTime _dateTime;
        private int _shiftNumber;
        private string _inn;
        private string _address;
        private string _website;
        private ObservableCollection<ReceiptItem> _items;
        private decimal _totalAmount;
        private decimal _vat20;
        private decimal _vat10;
        private string _email;
        private string _fiscalInfo;
        private string _qrCodeText;

        public int ReceiptNumber
        {
            get => _receiptNumber;
            set => Set(ref _receiptNumber, value);
        }

        public DateTime DateTime
        {
            get => _dateTime;
            set => Set(ref _dateTime, value);
        }

        public int ShiftNumber
        {
            get => _shiftNumber;
            set => Set(ref _shiftNumber, value);
        }

        public string Inn
        {
            get => _inn;
            set => Set(ref _inn, value);
        }

        public string Address
        {
            get => _address;
            set => Set(ref _address, value);
        }

        public string Website
        {
            get => _website;
            set => Set(ref _website, value);
        }

        public ObservableCollection<ReceiptItem> Items
        {
            get => _items;
            set => Set(ref _items, value);
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set => Set(ref _totalAmount, value);
        }

        public decimal Vat20
        {
            get => _vat20;
            set => Set(ref _vat20, value);
        }

        public decimal Vat10
        {
            get => _vat10;
            set => Set(ref _vat10, value);
        }

        public string Email
        {
            get => _email;
            set => Set(ref _email, value);
        }

        public string FiscalInfo
        {
            get => _fiscalInfo;
            set => Set(ref _fiscalInfo, value);
        }

        public string QrCodeText
        {
            get => _qrCodeText;
            set => Set(ref _qrCodeText, value);
        }

        public ReceiptViewModel(int saleId)
        {
            LoadReceiptData(saleId);
        }

        private void LoadReceiptData(int saleId)
        {
            using (var context = new KPMurtazinEntities())
            {
                var sale = context.Sales
                    .Include("SaleItems")
                    .Include("SaleItems.Products")
                    .Include("Employees")
                    .Include("Shifts")
                    .FirstOrDefault(s => s.SaleID == saleId);

                if (sale == null) return;

                ReceiptNumber = sale.SaleID;
                DateTime = sale.SaleDateTime;
                ShiftNumber = sale.ShiftID;
                Inn = "7725776121";
                Address = "127410, Москва г, Алтуфьевское ш., дом № 33Г";
                Website = "https://www.sin-say.com";
                Email = "contact.ru@sinsay.com";

                Items = new ObservableCollection<ReceiptItem>();
                foreach (var item in sale.SaleItems)
                {
                    Items.Add(new ReceiptItem
                    {
                        ProductName = item.Products.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Total = item.Quantity * item.UnitPrice,
                        TaxRate = item.UnitPrice > 100 ? 20 : 10,
                        TaxInfo = $"НДС {(item.UnitPrice > 100 ? "20" : "10")}/{(item.UnitPrice > 100 ? "120" : "110")}"
                    });
                }

                TotalAmount = sale.TotalAmount;

                // Расчет НДС
                Vat20 = Items.Where(i => i.TaxRate == 20).Sum(i => i.Total * 20 / 120);
                Vat10 = Items.Where(i => i.TaxRate == 10).Sum(i => i.Total * 10 / 110);

                FiscalInfo = $"№ АВТ.: 3010001\nСистема НО ОСН:\n№ ККТ: 0002609242062374\n№ ФН: 9960440301287084\n№ ФД: {saleId}\nФП: 1926279535";

                QrCodeText = $"t={DateTime:yyyyMMddTHHmmss}&s={TotalAmount:F2}&fn=9960440301287084&i={saleId}&fp=1926279535&n=1";
            }
        }
    }

    public class ReceiptItem : BaseViewModel
    {
        private string _productName;
        private int _quantity;
        private decimal _unitPrice;
        private decimal _total;
        private int _taxRate;
        private string _taxInfo;

        public string ProductName
        {
            get => _productName;
            set => Set(ref _productName, value);
        }

        public int Quantity
        {
            get => _quantity;
            set => Set(ref _quantity, value);
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set => Set(ref _unitPrice, value);
        }

        public decimal Total
        {
            get => _total;
            set => Set(ref _total, value);
        }

        public int TaxRate
        {
            get => _taxRate;
            set => Set(ref _taxRate, value);
        }

        public string TaxInfo
        {
            get => _taxInfo;
            set => Set(ref _taxInfo, value);
        }

        public string QuantityPrice => $"{Quantity} x {UnitPrice:F2}";
    }
}