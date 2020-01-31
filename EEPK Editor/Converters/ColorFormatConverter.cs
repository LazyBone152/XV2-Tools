using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace EEPK_Organiser.Converters
{
    /// <summary>
    /// For converting between byte and float color formats. (source: byte, target: float)
    /// </summary>
    public class ColorFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            float color = (float)value;
            return (int)(color * 255);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int color = System.Convert.ToInt32(value);
            return (float)color / 255;
        }
    }

}
