using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LB_Common.Converters
{
    public class VisibilityToNullableBooleanConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
            {
                return (((Visibility)value) == Visibility.Visible);
            }
            else
            {
                return Binding.DoNothing;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool?)
            {
                return (((bool?)value) == true ? Visibility.Visible : Visibility.Collapsed);
            }
            else if (value is bool)
            {
                return (((bool)value) == true ? Visibility.Visible : Visibility.Collapsed);
            }
            else
            {
                return Binding.DoNothing;
            }
        }
    }
}
