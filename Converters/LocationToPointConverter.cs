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

            double elementSize = 0;
            if (values.Length > 3 && values[3] is double size)
            {
                elementSize = size;
            }

            PointLatLng pointLatLng = new PointLatLng(lat, lon);
            Point screenPoint = map.FromLatLngToLocal(pointLatLng);

            if (parameter?.ToString() == "Y")
            {
                // Para la coordenada Y, resta la mitad de la altura del marcador para centrarlo
                return screenPoint.Y - (elementSize / 2);
            }
            else
            {
                // Para la coordenada X, resta la mitad del ancho del marcador para centrarlo
                return screenPoint.X - (elementSize / 2);
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}