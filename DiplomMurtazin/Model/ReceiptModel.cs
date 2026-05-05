using System;
using System.Collections.Generic;

namespace DiplomMurtazin.Model
{
    public class ReceiptModel
    {
        public int SaleNumber { get; set; }
        public int ShiftNumber { get; set; }
        public string Cashier { get; set; } = "АДМИНИСТРАТОР";
        public DateTime DateTime { get; set; }

        public List<ReceiptItem> Items { get; set; } = new List<ReceiptItem>();
        public decimal TotalAmount { get; set; }
        public decimal AmountWithoutVat { get; set; }
        public decimal CashPayment { get; set; }

        public string CompanyName { get; set; } = "ИП Рахимова Ирина Кырбангалиевна";
        public string Address { get; set; } = "452946, РЕСП. Башкортостан, М.-Р-Н Краснокамский, с. Куяново, ул. Цветочная, д. 1";
        public string Place { get; set; } = "Салон связи диксис";
        public string Website { get; set; } = "www.nalog.gov.ru";

        public string RnKkt { get; set; } = "00007530073063269";
        public string ZnKkt { get; set; } = "+00307901516520+";
        public string FnNumber { get; set; } = "9961440300619282";
        public string Sno { get; set; } = "УСН ДОХОД";
        public int FdNumber { get; set; }
        public string Fp { get; set; }
        public string Inn { get; set; } = "023101529840";
        public int DocumentNumber { get; set; }
    }

    public class ReceiptItem
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total => Price * Quantity;
    }
}