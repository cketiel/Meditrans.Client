using Meditrans.Client.Models;
using Meditrans.Client.DTOs;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections.ObjectModel; // Necesario para la colección de ScheduleDto

namespace Meditrans.Client.ViewModels
{
    public class RunItemViewModel : BaseViewModel
    {
        private VehicleRouteViewModel _vehicleRoute;
        public VehicleRouteViewModel VehicleRoute
        {
            get => _vehicleRoute;
            set => SetProperty(ref _vehicleRoute, value);
        }

        private ObservableCollection<ScheduleDto> _schedules;
        public ObservableCollection<ScheduleDto> Schedules
        {
            get => _schedules;
            set => SetProperty(ref _schedules, value);
        }

        public RunItemViewModel(VehicleRouteViewModel vehicleRoute)
        {
            _vehicleRoute = vehicleRoute;
            Schedules = new ObservableCollection<ScheduleDto>();
        }
       
        public int IsOn { get; set; } = 0; // preguntar como se calcula
        public int MTE { get; set; } = 0; // preguntar como se calcula
        public int ME { get; set; } = 0; // preguntar como se calcula

        public int TripsCount => Schedules.Where(s => s.TripId.HasValue).GroupBy(s => s.TripId).Count();

        public int UnperformedCount => Schedules.Count(s => !s.Performed && s.TripId.HasValue);


        public string Name => VehicleRoute.Name;
        public string DriverFullName => VehicleRoute.Driver; //?.FullName;
        public string VehicleName => VehicleRoute.Vehicle; //?.Name;

        // Lógica para Last Event, Next Event
        public string LastNextEvent
        {
            get
            {
                if (!Schedules.Any()) return "No Events";

                var orderedEvents = Schedules.OrderBy(s => s.Pickup ?? s.Appt).ToList();

                var lastEvent = orderedEvents.LastOrDefault(s => s.Performed);
                var nextEvent = orderedEvents.FirstOrDefault(s => !s.Performed);

                string lastEventText = lastEvent != null ? $"{lastEvent.Name}" : "N/A";
                string nextEventText = nextEvent != null ? $"{nextEvent.Name}" : "N/A";

                //string lastEventText = lastEvent != null ? $"{lastEvent.PerformedTimeFormatted} - {lastEvent.EventType}" : "N/A";
                //string nextEventText = nextEvent != null ? $"{nextEvent.ScheduledTimeFormatted} - {nextEvent.EventType}" : "N/A";

                return $"Last: {lastEventText}\nNext: {nextEventText}";
            }
        }
    }

    // Extensión para ScheduleDto para facilitar la visualización de tiempos
    public static class ScheduleDtoExtensions
    {
        public static string ScheduledTimeFormatted(this ScheduleDto dto)
        {
            if (dto.Pickup.HasValue) return dto.Pickup.Value.ToString(@"hh\:mm");
            if (dto.Appt.HasValue) return dto.Appt.Value.ToString(@"hh\:mm");
            return "N/A";
        }

        public static string PerformedTimeFormatted(this ScheduleDto dto)
        {
            if (dto.Perform.HasValue) return dto.Perform.Value.ToString(@"hh\:mm");
            if (dto.Arrive.HasValue) return dto.Arrive.Value.ToString(@"hh\:mm");
            return "N/A";
        }
    }
}