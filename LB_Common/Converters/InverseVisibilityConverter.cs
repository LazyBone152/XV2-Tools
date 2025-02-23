using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LB_Common.Converters
{
    public class InverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return InvertVisibiity(visibility);
            }
            else
            {
                return Binding.DoNothing;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return InvertVisibiity(visibility);
            }
            else
            {
                return Binding.DoNothing;
            }
        }
    
        private static Visibility InvertVisibiity(Visibility visibility)
        {
            return visibility == Visibility.Collapsed || visibility == Visibility.Hidden ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
