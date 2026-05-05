using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DiplomMurtazin.Core
{
    public class StatusBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isError && isError)
                return new SolidColorBrush(Color.FromRgb(231, 76, 60)); // #e74c3c

            return new SolidColorBrush(Color.FromRgb(52, 152, 219)); // #3498db
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}