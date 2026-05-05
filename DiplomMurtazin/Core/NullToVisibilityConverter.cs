using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DiplomMurtazin.Core
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Если parameter = "invert", то инвертируем логику
            bool invert = parameter as string == "invert";

            if (invert)
                return value == null ? Visibility.Visible : Visibility.Collapsed;
            else
                return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}