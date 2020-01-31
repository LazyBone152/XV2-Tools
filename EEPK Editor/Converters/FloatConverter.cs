using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace EEPK_Organiser.Converters
{
    [ValueConversion(typeof(double), typeof(string))]
    public class DoubleToStringConverter : IValueConverter
    {
        #region IValueConverter Members

        public DoubleToStringConverter()
        {
            // Default value for DigitsCount
            DigitsCount = 7;
        }

        // Convert from double to string
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;

            double doubleValue = System.Convert.ToDouble(value);
            return doubleValue.ToString();
        }

        // Convert from string to double
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;
            
            string str = System.Convert.ToString(value);

            try
            {
                if (str[str.Length - 1] == '.')
                {
                    return System.Convert.ToDouble(string.Format("{0}.0", str));
                }
                else
                {
                    return System.Convert.ToDouble(str);
                }
            }
            catch
            {

            }

            return DependencyProperty.UnsetValue;
        }

        public int DigitsCount { get; set; }

        #endregion
    }
}
