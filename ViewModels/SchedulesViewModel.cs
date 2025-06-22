using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Spreadsheet;
using Meditrans.Client.DTOs;
using Meditrans.Client.Models;
using Meditrans.Client.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace Meditrans.Client.ViewModels
{
    public partial class SchedulesViewModel : ObservableObject
    {
        private readonly ScheduleService _scheduleService;

        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Today;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadSchedulesAndTripsCommand))] 
        [NotifyCanExecuteChangedFor(nameof(RouteTripCommand))]
        private VehicleRoute _selectedVehicleRoute;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RouteTripCommand))]
        private UnscheduledTripDto _selectedUnscheduledTrip;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CancelRouteCommand))]
        private ScheduleDto _selectedSchedule;

        public ObservableCollection<VehicleRoute> VehicleRoutes { get; } = new();
        public ObservableCollection<VehicleGroup> VehicleGroups { get; } = new();
        public ObservableCollection<ScheduleDto> Schedules { get; } = new();
        public ObservableCollection<UnscheduledTripDto> UnscheduledTrips { get; } = new();

        public SchedulesViewModel(ScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
            LoadInitialDataCommand = new AsyncRelayCommand(LoadInitialDataAsync);
            LoadSchedulesAndTripsCommand = new AsyncRelayCommand(LoadSchedulesAndTripsAsync, CanLoadSchedulesAndTrips);
            RouteTripCommand = new AsyncRelayCommand(RouteSelectedTripAsync, CanRouteSelectedTrip);
            CancelRouteCommand = new AsyncRelayCommand(CancelSelectedRouteAsync, CanCancelSelectedRoute);

            _ = InitializeAsync();
        }

        public IAsyncRelayCommand LoadInitialDataCommand { get; }
        public IAsyncRelayCommand LoadSchedulesAndTripsCommand { get; }
        public IAsyncRelayCommand RouteTripCommand { get; }
        public IAsyncRelayCommand CancelRouteCommand { get; }

        private async Task InitializeAsync()
        {
            try
            {
                await LoadInitialDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading initial data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"Error loading initial data: {ex.Message}");
            }
        }
        // Load initial data (route and group lists)
        private async Task LoadInitialDataAsync()
        {
            RunService _runService = new RunService();
            var routes = await _runService.GetAllAsync();         
            foreach (var route in routes) VehicleRoutes.Add(route);

            VehicleGroupService _vehicleGroupService = new VehicleGroupService();
            var groups = await _vehicleGroupService.GetGroupsAsync();
            VehicleGroups.Clear();
            foreach (var group in groups)
            {
                VehicleGroups.Add(group);
            }
        }

        // Load the main grids
        private async Task LoadSchedulesAndTripsAsync()
        {
            Schedules.Clear();
            UnscheduledTrips.Clear();

            var schedules = await _scheduleService.GetSchedulesAsync(SelectedVehicleRoute.Id, SelectedDate);
            foreach (var s in schedules) Schedules.Add(s);

            var trips = await _scheduleService.GetUnscheduledTripsAsync(SelectedDate);
            foreach (var t in trips) UnscheduledTrips.Add(t);
        }

        private bool CanLoadSchedulesAndTrips() => SelectedVehicleRoute != null;

        // Routing logic
        private async Task RouteSelectedTripAsync()
        {
            await _scheduleService.RouteTripsAsync(SelectedVehicleRoute.Id, new List<int> { SelectedUnscheduledTrip.Id });

            // Refresh data
            await LoadSchedulesAndTripsAsync();
        }

        private bool CanRouteSelectedTrip() => SelectedVehicleRoute != null && SelectedUnscheduledTrip != null;

        // Logic to cancel
        private async Task CancelSelectedRouteAsync()
        {
            await _scheduleService.CancelRouteAsync(SelectedSchedule.Id);

            // Refresh data
            await LoadSchedulesAndTripsAsync();
        }

        private bool CanCancelSelectedRoute() => SelectedSchedule != null;

        // Observe changes to automatically refresh data
        partial void OnSelectedDateChanged(DateTime value)
        {
            if (CanLoadSchedulesAndTrips())
                LoadSchedulesAndTripsCommand.Execute(null);
        }

        partial void OnSelectedVehicleRouteChanged(VehicleRoute value)
        {
            if (CanLoadSchedulesAndTrips())
                LoadSchedulesAndTripsCommand.Execute(null);
        }
    }
}
