using Meditrans.Client.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Meditrans.Client.Converters
{
    public class EtaViolationConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // We expect 4 values: EventType, ETA, Pickup, Appt
            if (values.Length < 4)
            {
                return false;
            }

            
            if (!(values[0] is ScheduleEventType eventType))
            {
                return false;
            }
            if (!(values[1] is TimeSpan eta))
            {
                return false;
            }

            // Check violation based on event type
            if (eventType == ScheduleEventType.Pickup)
            {
                
                if (values[2] is TimeSpan pickup)
                {
                    return eta > pickup;
                }
            }
            else if (eventType == ScheduleEventType.Dropoff)
            {
                
                if (values[3] is TimeSpan appt)
                {
                    return eta > appt;
                }
            }

            // If the time values ​​(Pickup/Appt) are null or the EventType does not match,
            // the code will arrive here and return false, indicating that there is no violation.
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}