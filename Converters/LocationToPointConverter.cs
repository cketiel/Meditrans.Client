using GMap.NET;
using GMap.NET.WindowsPresentation;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Meditrans.Client.Converters
{
    public class LocationToPointConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3 ||
                !(values[0] is GMapControl map) ||
                !double.TryParse(values[1]?.ToString(), out double lat) ||
                !double.TryParse(values[2]?.ToString(), out double lon))
            {
                return DependencyProperty.UnsetValue;
            }

            if (!map.IsLoaded)
            {
                return DependencyProperty.UnsetValue;
            }

            try
            {
                double elementSize = 0;
                if (values.Length > 3 && values[3] is double size)
                {
                    elementSize = size;
                }

                PointLatLng pointLatLng = new PointLatLng(lat, lon);
               
                GPoint gPoint = map.FromLatLngToLocal(pointLatLng);
             
                Point screenPoint = new Point(gPoint.X, gPoint.Y);
                

                if (parameter?.ToString() == "Y")
                {
                    return screenPoint.Y - (elementSize / 2);
                }
                else
                {
                    return screenPoint.X - (elementSize / 2);
                }
            }
            catch (Exception)
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}