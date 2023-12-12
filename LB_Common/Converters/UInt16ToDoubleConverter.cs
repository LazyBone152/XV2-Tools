using System;
using System.Globalization;
using System.Windows.Data;

namespace LB_Common.Converters
{
    /// <summary>
    /// Convert UShort into Double, properly accounting for the min and max value ranges.
    /// </summary>
    public class UInt16ToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double?)
            {
                double val = (double)(double?)value;

                if (val < 0) return ushort.MaxValue;
                if(val > ushort.MaxValue) return ushort.MinValue;
                return (ushort)val;
            }
            else
            {
                return Binding.DoNothing;
            }
        }
    }
}
