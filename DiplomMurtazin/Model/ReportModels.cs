using System;

namespace DiplomMurtazin.Model
{
    public class SalesReportItem
    {
        public int SaleId { get; set; }
        public DateTime SaleDate { get; set; }
        public string EmployeeName { get; set; }
        public string PaymentMethod { get; set; }
        public int ItemsCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class SalesSummaryItem
    {
        public string EmployeeName { get; set; }
        public int SalesCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageCheck { get; set; }
    }

    public class StockReportItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Category { get; set; }
        public int CurrentStock { get; set; }
        public int MinStockLevel { get; set; }
        public string Status { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class ProductCatalogItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Category { get; set; }
        public string Barcode { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public decimal UnitPrice { get; set; }
        public int WarrantyMonths { get; set; }
        public int CurrentStock { get; set; }
    }
}