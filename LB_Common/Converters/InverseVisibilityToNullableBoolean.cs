using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LB_Common.Converters
{
    public class InverseVisibilityToNullableBooleanConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
            {
                return (((Visibility)value) == Visibility.Collapsed);
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
                return (((bool?)value) == true ? Visibility.Collapsed : Visibility.Visible);
            }
            else if (value is bool)
            {
                return (((bool)value) == true ? Visibility.Collapsed : Visibility.Visible);
            }
            else
            {
                return Binding.DoNothing;
            }
        }
    }
}
