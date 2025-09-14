using CommunityToolkit.Mvvm.ComponentModel;
using DocumentFormat.OpenXml.Wordprocessing;
using Meditrans.Client.Models; 
using System;

namespace Meditrans.Client.DTOs
{
    public partial class ScheduleDto : ObservableObject
    {
        // Propiedades que no cambian o no se necesita notificar en tiempo real
        public int Id { get; set; }
        public int? TripId { get; set; }
        public string Name { get; set; }
        //public TimeSpan? Pickup { get; set; }
        //public TimeSpan? Appt { get; set; }
        public string Address { get; set; }
        public double ScheduleLatitude { get; set; }
        public double ScheduleLongitude { get; set; }
        public string? Comment { get; set; }
        public string? Phone { get; set; }
        public TimeSpan? Arrive { get; set; }
        public TimeSpan? Perform { get; set; }
        public double? ArriveDist { get; set; }
        public double? PerformDist { get; set; }
        public string? Driver { get; set; }
        public string? GPSArrive { get; set; }
        public long? Odometer { get; set; }
        public string? AuthNo { get; set; }
        public string? FundingSource { get; set; }
        public DateTime? Date { get; set; }
        //public ScheduleEventType? EventType { get; set; }
        public string? SpaceType { get; set; }
        public string? TripType { get; set; }
        public string? Patient { get; set; }
        public bool Performed { get; set; }
        public string? Run { get; set; }
        public string? Vehicle { get; set; }

        // --- Properties that DO change and need to notify the UI ---
        // They are converted to private fields with the attribute [ObservableProperty]

        [ObservableProperty]
        private ScheduleEventType? _eventType;

        [ObservableProperty]
        private TimeSpan? _pickup;

        [ObservableProperty]
        private TimeSpan? _appt;

        [ObservableProperty]
        private TimeSpan? _eTA;

        [ObservableProperty]
        private double? _distance;

        [ObservableProperty]
        private TimeSpan? _travel;

        [ObservableProperty]
        private int? _on; 

        [ObservableProperty]
        private int? _sequence;

        [ObservableProperty]
        private bool _isSelectedForMap;
    }
}