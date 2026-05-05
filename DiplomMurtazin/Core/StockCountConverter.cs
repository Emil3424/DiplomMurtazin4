using DiplomMurtazin.Model;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace DiplomMurtazin.Core
{
    public class StockCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not System.Collections.IEnumerable collection) return 0;

            var items = collection.Cast<StockReportItem>().ToList();
            string param = parameter as string ?? "";

            if (param == "zero")
                return items.Count(s => s.CurrentStock == 0);
            else if (param == "low")
                return items.Count(s => s.Status == "Ниже минимума");

            return items.Count;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}