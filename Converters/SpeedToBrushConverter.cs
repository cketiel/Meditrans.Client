using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Meditrans.Client.Converters
{
    public class SpeedToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double speed)
            {
                // If the speed is greater than, say, 2 mph, it is in motion.
                if (speed > 2.0)
                {
                    return new SolidColorBrush(Colors.DodgerBlue); // On the move
                }
                else
                {
                    return new SolidColorBrush(Colors.SlateGray); // Stopped
                }
            }
            return new SolidColorBrush(Colors.SlateGray); // Default value
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
