using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Spreadsheet;
using Meditrans.Client.DTOs;
using Meditrans.Client.Models;
using Meditrans.Client.Services;
using System.Collections.ObjectModel;
using System.Windows;
using Meditrans.Client.Views.Schedules;
using System.Windows.Input;

namespace Meditrans.Client.ViewModels
{
    public partial class SchedulesViewModel : ObservableObject
    {
        //private readonly UserConfigService _userConfigService;
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

        // The code generator will create a public ColumnConfigurations property.
        // Every time you assign a new value to ColumnConfigurations,
        // OnPropertyChanged(nameof(ColumnConfigurations)) will be called,
        // which will force the UI to re-evaluate all bindings that depend on this property.
        [ObservableProperty]
        private ObservableCollection<ColumnConfig> _columnConfigurations = new();
        // public ObservableCollection<ColumnConfig> ColumnConfigurations { get; set; } = new();

        public SchedulesViewModel(ScheduleService scheduleService)
        {
            //UserConfigService _userConfigService = new UserConfigService();
            _scheduleService = scheduleService;
            LoadInitialDataCommand = new AsyncRelayCommand(LoadInitialDataAsync);
            LoadSchedulesAndTripsCommand = new AsyncRelayCommand(LoadSchedulesAndTripsAsync, CanLoadSchedulesAndTrips);
            RouteTripCommand = new AsyncRelayCommand(RouteSelectedTripAsync, CanRouteSelectedTrip);
            CancelRouteCommand = new AsyncRelayCommand(CancelSelectedRouteAsync, CanCancelSelectedRoute);
            OpenColumnSelectorCommand = new RelayCommand(OpenColumnSelector);

            InitializeColumns();
            _ = InitializeAsync();
        }

        public IAsyncRelayCommand LoadInitialDataCommand { get; }
        public IAsyncRelayCommand LoadSchedulesAndTripsCommand { get; }
        public IAsyncRelayCommand RouteTripCommand { get; }
        public IAsyncRelayCommand CancelRouteCommand { get; }
        public ICommand OpenColumnSelectorCommand { get; }

        private void OpenColumnSelector()
        {
            // Action function that the popup ViewModel will use to close.
            Action closeAction = null;

            var viewModel = new ScheduleColumnSelectorViewModel(ColumnConfigurations, () => closeAction?.Invoke());
            var view = new ColumnSelectorView
            {
                DataContext = viewModel,
                //Owner = Application.Current.MainWindow // Assign the main window as the owner
            };

            closeAction = () => view.Close();

            view.ShowDialog();

            if (viewModel.DialogResult == true)
            {
                // 1. Replaces the entire collection. This will trigger the UI update.
                ColumnConfigurations = new ObservableCollection<ColumnConfig>(viewModel.Columns);

                // The new configuration is saved for future sessions.
                UserConfigService _userConfigService = new UserConfigService();
                _userConfigService.SaveColumnConfig(ColumnConfigurations);

                // The user pressed OK. The main configuration is updated.
                /*foreach (var updatedConfig in viewModel.Columns)
                {
                    var originalConfig = ColumnConfigurations.FirstOrDefault(c => c.PropertyName == updatedConfig.PropertyName);
                    if (originalConfig != null)
                    {
                        originalConfig.IsVisible = updatedConfig.IsVisible;
                    }
                }

                // The new configuration is saved for future sessions.
                UserConfigService _userConfigService = new UserConfigService();
                _userConfigService.SaveColumnConfig(ColumnConfigurations);*/
            }
        }
        private void InitializeColumns()
        {
            // Try to load user saved settings
            UserConfigService _userConfigService = new UserConfigService();
            var savedConfig = _userConfigService.LoadColumnConfig();

            if (savedConfig != null)
            {
                foreach (var config in savedConfig)
                {
                    ColumnConfigurations.Add(config);
                }
            }
            else
            {
                // If there is no saved configuration, it creates the default configuration.
                // The PropertyName MUST match the property name in ScheduleDto.
                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "Action", Header = "Action", IsVisible = true });
                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "Name", Header = "Name", IsVisible = true });
                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "Pickup", Header = "Pickup", IsVisible = true });
                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "Appt", Header = "Appt", IsVisible = true });
                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "ETA", Header = "ETA", IsVisible = true });
                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "Distance", Header = "Distance", IsVisible = true });
                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "Travel", Header = "Travel", IsVisible = true });
                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "On", Header = "On", IsVisible = true });
                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "SpaceTypeName", Header = "Space", IsVisible = true });
                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "Address", Header = "Address", IsVisible = true });

                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "Comment", Header = "Comment", IsVisible = true });
                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "Phone", Header = "Phone", IsVisible = true });
                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "Arrive", Header = "Arrive", IsVisible = true });
                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "Perform", Header = "Perform", IsVisible = true });
                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "ArriveDist", Header = "ArriveDist", IsVisible = true });
                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "PerformDist", Header = "PerformDist", IsVisible = true });
                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "Driver", Header = "Driver", IsVisible = true });
                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "GPSArrive", Header = "GPS Arrive", IsVisible = true });
                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "Odometer", Header = "Odometer", IsVisible = true });
                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "AuthNo", Header = "AuthNo", IsVisible = true });

                ColumnConfigurations.Add(new ColumnConfig { PropertyName = "FundingSource", Header = "Funding Source", IsVisible = true });
            }
        }
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
            var TripToSchedule = SelectedUnscheduledTrip;
            var run = SelectedVehicleRoute;
            GoogleMapsService _googleMapsService = new GoogleMapsService();

            // Check if the trip and vehicle route are selected
            if (TripToSchedule == null || run == null)
            {
                MessageBox.Show("Please select a trip and a vehicle route before routing.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            double? pDistance = 0;
            TimeSpan? pTravelTime = TimeSpan.Zero;
            TimeSpan? pETA = TimeSpan.Zero;

            double? dDistance = 0;
            TimeSpan? dTravelTime = TimeSpan.Zero;
            TimeSpan? dETA = TimeSpan.Zero;

            bool isFirstEvent = Schedules.Count == 0;
            bool editEvent = false; // Flag to indicate if we need to edit an event

            double PullInPreviousLatitude = 0.0;
            double PullInPreviousLongitude = 0.0;
            TimeSpan? PullInPreviousETA = TimeSpan.Zero;

            TimeSpan? violationSetLimit = TimeSpan.Zero;

            if (isFirstEvent)
            {
                var pickupFullDetails = await _googleMapsService.GetRouteFullDetails(
                    run.GarageLatitude, 
                    run.GarageLongitude, 
                    TripToSchedule.PickupLatitude, 
                    TripToSchedule.PickupLongitude);
                var dropoffFullDetails = await _googleMapsService.GetRouteFullDetails(
                    TripToSchedule.PickupLatitude, 
                    TripToSchedule.PickupLongitude, 
                    TripToSchedule.DropoffLatitude,
                    TripToSchedule.DropoffLongitude);
                
                if (pickupFullDetails != null && dropoffFullDetails != null)
                {
                    pDistance = pickupFullDetails.DistanceMiles;
                    pTravelTime = TimeSpan.FromSeconds(pickupFullDetails.DurationInTrafficSeconds);
                    pETA = TripToSchedule.FromTime - TimeSpan.FromMinutes(15);

                    dDistance = dropoffFullDetails.DistanceMiles; // TripToSchedule.Distance;
                    dTravelTime = TimeSpan.FromSeconds(dropoffFullDetails.DurationInTrafficSeconds);
                    dETA = TripToSchedule.FromTime + dTravelTime + TimeSpan.FromMinutes(15);
                }
                
            }
            else 
            {
                TimeSpan? pickupTime = TripToSchedule.FromTime; // pickup time of the trip to be routed             
                var pickupEvent = Schedules[0];
                var dropoffEvent = Schedules[1];
                var EventToEdit = Schedules[^1];
                var PullInEvent = Schedules[^1];
                var PreviousPullInEvent = Schedules[Schedules.Count - 2];
               
                // Get the last scheduled trip
                var lastScheduledTrip = Schedules[^1]; // Get the last item in the list
               

                int start = Schedules.Count - 3;
                int end = 1;
                for (int i = start; i >= end; i-=2)
                {
                    // Lo mas temprano que se puede llegar antes en Appt es 15 min. , en un Return 5 min
                    if (Schedules[i].TripType == TripType.Appointment)
                    {
                        violationSetLimit = TripToSchedule.FromTime - TimeSpan.FromMinutes(15);
                    }
                    else // TripType.Return
                    {
                        violationSetLimit = TripToSchedule.FromTime - TimeSpan.FromMinutes(5);
                    }

                    TimeSpan? referenceTime = TimeSpan.Zero;
                    TimeSpan? referenceTimeD = TimeSpan.Zero;

                    pickupEvent = Schedules[i]; 
                    dropoffEvent = Schedules[i + 1]; 
                    if (pickupTime > pickupEvent.Pickup) // If the pickup time of the trip to be routed is later than the pickup time of the last event
                    {
                        double olat = dropoffEvent.ScheduleLatitude;
                        double olng = dropoffEvent.ScheduleLongitude;
                        //Porque si es  TripType.Return el dropoff no tiene appt
                        if (Schedules[i].TripType == TripType.Appointment && (pickupTime > dropoffEvent.Appt || pickupTime == dropoffEvent.Appt))
                        {
                            referenceTime = dropoffEvent.ETA;
                            olat = dropoffEvent.ScheduleLatitude;
                            olng = dropoffEvent.ScheduleLongitude;
                        }
                        else if (Schedules[i].TripType == TripType.Appointment && pickupTime < dropoffEvent.Appt)
                        {
                            olat = pickupEvent.ScheduleLatitude;
                            olng = pickupEvent.ScheduleLongitude;
                            referenceTime = pickupEvent.ETA;
                        }
                        
                        var pickupFullDetails = await _googleMapsService.GetRouteFullDetails(
                            olat,
                            olng,
                            TripToSchedule.PickupLatitude,
                            TripToSchedule.PickupLongitude);

                        double olatD = TripToSchedule.PickupLatitude;
                        double olngD = TripToSchedule.PickupLongitude;
                        bool flag = false;

                        TimeSpan? pickupAppt = TripToSchedule.ToTime;
                        if (Schedules[i].TripType == TripType.Appointment)
                        {
                            if (pickupAppt < dropoffEvent.Appt) // trip to schedule order
                            {
                                flag = false;//referenceTimeD = dropoffEvent.ETA;
                                olatD = TripToSchedule.PickupLatitude;
                                olngD = TripToSchedule.PickupLongitude;
                            }
                            else // se alterna
                            {                               
                                flag = true;
                                olatD = dropoffEvent.ScheduleLatitude;
                                olngD = dropoffEvent.ScheduleLongitude;
                            }
                        }

                        var dropoffFullDetails = await _googleMapsService.GetRouteFullDetails(
                            olatD,
                            olngD,
                            TripToSchedule.DropoffLatitude,
                            TripToSchedule.DropoffLongitude);

                        


                        if (pickupFullDetails != null && dropoffFullDetails != null)
                        {
                            pDistance = pickupFullDetails.DistanceMiles;
                            pTravelTime = TimeSpan.FromSeconds(pickupFullDetails.DurationInTrafficSeconds);
                            // Lo mas temprano que se puede llegar antes en Appt es 15 min. , en un Return 5 min
                            TimeSpan? realETA = referenceTime + pTravelTime;                            
                            pETA = (realETA < violationSetLimit) ? violationSetLimit : realETA;

                            dDistance = dropoffFullDetails.DistanceMiles;
                            dTravelTime = TimeSpan.FromSeconds(dropoffFullDetails.DurationInTrafficSeconds);
                            referenceTimeD = flag ? pETA : dropoffEvent.ETA;
                            dETA = referenceTimeD + dTravelTime + TimeSpan.FromMinutes(15);
                        }

                        if (editEvent) 
                        {
                            if (EventToEdit.TripType == TripType.Appointment)
                            {
                                violationSetLimit = EventToEdit.Pickup - TimeSpan.FromMinutes(15);
                            }
                            else // TripType.Return
                            {
                                violationSetLimit = EventToEdit.Pickup - TimeSpan.FromMinutes(5);
                            }

                            var editEventFullDetails = await _googleMapsService.GetRouteFullDetails(
                            TripToSchedule.DropoffLatitude,
                            TripToSchedule.DropoffLongitude,
                            EventToEdit.ScheduleLatitude,
                            EventToEdit.ScheduleLongitude);

                            EventToEdit.Distance = editEventFullDetails.DistanceMiles;
                            EventToEdit.Travel = TimeSpan.FromSeconds(editEventFullDetails.DurationInTrafficSeconds);
                            //EventToEdit.ETA = EventToEdit.Pickup - TimeSpan.FromMinutes(15);
                            TimeSpan? realETA = dETA + EventToEdit.Travel;
                            EventToEdit.ETA = (realETA < violationSetLimit) ? violationSetLimit : realETA;

                            await _scheduleService.UpdateAsync(EventToEdit.Id, EventToEdit);
                            editEvent = false; // Reset the flag after editing the event

                            PullInPreviousLatitude = PreviousPullInEvent.ScheduleLatitude;
                            PullInPreviousLongitude = PreviousPullInEvent.ScheduleLongitude;
                            PullInPreviousETA = PreviousPullInEvent.ETA;
                        }
                        else 
                        {
                            PullInPreviousLatitude = TripToSchedule.DropoffLatitude;
                            PullInPreviousLongitude = TripToSchedule.DropoffLongitude;
                            PullInPreviousETA = dETA;
                        }
                        break;
                    }
                    else 
                    {
                        editEvent = true;
                        EventToEdit = pickupEvent; // If the pickup time of the trip to be routed is earlier than the pickup time of the last event, edit the last event
                    }
                }
                // It means that the pick-up time is the first of all the schedules, following of course the Pull-out
                if (editEvent)
                {                  

                    if (TripToSchedule.Type == TripType.Appointment)
                    {
                        violationSetLimit = TripToSchedule.FromTime - TimeSpan.FromMinutes(15);
                    }
                    else // TripType.Return
                    {
                        violationSetLimit = TripToSchedule.FromTime - TimeSpan.FromMinutes(5);
                    }

                    var pickupFullDetails = await _googleMapsService.GetRouteFullDetails(
                        run.GarageLatitude,
                        run.GarageLongitude,
                        TripToSchedule.PickupLatitude,
                        TripToSchedule.PickupLongitude);
                    var dropoffFullDetails = await _googleMapsService.GetRouteFullDetails(
                        TripToSchedule.PickupLatitude,
                        TripToSchedule.PickupLongitude,
                        TripToSchedule.DropoffLatitude,
                        TripToSchedule.DropoffLongitude);

                    if (pickupFullDetails != null && dropoffFullDetails != null)
                    {
                        pDistance = pickupFullDetails.DistanceMiles;
                        pTravelTime = TimeSpan.FromSeconds(pickupFullDetails.DurationInTrafficSeconds);
                        pETA = TripToSchedule.FromTime - TimeSpan.FromMinutes(15);
                        
                        dDistance = dropoffFullDetails.DistanceMiles; // TripToSchedule.Distance;
                        dTravelTime = TimeSpan.FromSeconds(dropoffFullDetails.DurationInTrafficSeconds);
                        dETA = TripToSchedule.FromTime + dTravelTime + TimeSpan.FromMinutes(15);
                    }

                    // Edit the ETA of the Pull-out event
                    var PullOutEvent = Schedules[0];
                    PullOutEvent.ETA = TripToSchedule.FromTime - (TimeSpan.FromMinutes(20) + pTravelTime);
                    await _scheduleService.UpdateAsync(PullOutEvent.Id, PullOutEvent);

                    var editEventFullDetails = await _googleMapsService.GetRouteFullDetails(
                        TripToSchedule.DropoffLatitude,
                        TripToSchedule.DropoffLongitude,
                        EventToEdit.ScheduleLatitude,
                        EventToEdit.ScheduleLongitude);

                    EventToEdit.Distance = editEventFullDetails.DistanceMiles;
                    EventToEdit.Travel = TimeSpan.FromSeconds(editEventFullDetails.DurationInTrafficSeconds);
                    //EventToEdit.ETA = EventToEdit.Pickup - TimeSpan.FromMinutes(15);                   
                    TimeSpan? realETA = dETA + EventToEdit.Travel;
                    EventToEdit.ETA = (realETA < violationSetLimit) ? violationSetLimit : realETA;

                    await _scheduleService.UpdateAsync(EventToEdit.Id, EventToEdit);
                    editEvent = false; // Reset the flag after editing the event

                    PullInPreviousLatitude = PreviousPullInEvent.ScheduleLatitude;
                    PullInPreviousLongitude = PreviousPullInEvent.ScheduleLongitude;
                    PullInPreviousETA = PreviousPullInEvent.ETA;

                }

                // Always edit the Pull-in event
                var editPullInEventFullDetails = await _googleMapsService.GetRouteFullDetails(
                        PullInPreviousLatitude, //PreviousPullInEvent.ScheduleLatitude,
                        PullInPreviousLongitude, //PreviousPullInEvent.ScheduleLongitude,
                        PullInEvent.ScheduleLatitude,
                        PullInEvent.ScheduleLongitude);

                PullInEvent.Distance = editPullInEventFullDetails.DistanceMiles;
                PullInEvent.Travel = TimeSpan.FromSeconds(editPullInEventFullDetails.DurationInTrafficSeconds);
                PullInEvent.ETA = PullInPreviousETA + PullInEvent.Travel; //PreviousPullInEvent.ETA + PullInEvent.Travel;

                await _scheduleService.UpdateAsync(PullInEvent.Id, PullInEvent);

            }

            var request = new RouteTripRequest
            {
                VehicleRouteId = SelectedVehicleRoute.Id,
                TripId = SelectedUnscheduledTrip.Id,
                PickupDistance = pDistance.Value,
                PickupTravelTime = pTravelTime.Value,
                PickupETA = pETA.Value,
                DropoffDistance = dDistance.Value,
                DropoffTravelTime = dTravelTime.Value,
                DropoffETA = dETA.Value

            };
            try
            {
                await _scheduleService.RouteTripsAsync(request);
                //await _scheduleService.RouteTripsAsync(SelectedVehicleRoute.Id, new List<int> { SelectedUnscheduledTrip.Id });

                // Refresh data
                await LoadSchedulesAndTripsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                        string.Format(TripRoutingError, ex.Message),
                        ErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                //MessageBox.Show($"Trip routing error: {ex.Message}", "Error");
            }

        }

        private bool CanRouteSelectedTrip() => SelectedVehicleRoute != null && SelectedUnscheduledTrip != null;

        // Logic to cancel
        private async Task CancelSelectedRouteAsync()
        {
            try
            {
                await _scheduleService.CancelRouteAsync(SelectedSchedule.Id);

                // Refresh data
                await LoadSchedulesAndTripsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                        string.Format(CancelScheduleError, ex.Message),
                        ErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                //MessageBox.Show($"Error canceling Schedule: {ex.Message}", "Error");
            }

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

        #region Translation

        // Schedules Grid
        public string ColumnHeaderName => LocalizationService.Instance["Name"];
        public string ColumnHeaderPickup => LocalizationService.Instance["Pickup"];
        public string ColumnHeaderAppt => LocalizationService.Instance["Appt"];
        public string ColumnHeaderETA => LocalizationService.Instance["ETA"];
        public string ColumnHeaderDistance => LocalizationService.Instance["Distance"];
        public string ColumnHeaderTravel => LocalizationService.Instance["Travel"];
        public string ColumnHeaderOn => LocalizationService.Instance["On"];
        public string ColumnHeaderSpace => LocalizationService.Instance["Space"];
        public string ColumnHeaderAddress => LocalizationService.Instance["Address"];
        public string ColumnHeaderComment => LocalizationService.Instance["Comment"];
        public string ColumnHeaderPhone => LocalizationService.Instance["Phone"];
        public string ColumnHeaderArrive => LocalizationService.Instance["Arrive"];
        public string ColumnHeaderPerform => LocalizationService.Instance["Perform"];
        public string ColumnHeaderArriveDist => LocalizationService.Instance["ArriveDist"];
        public string ColumnHeaderPerformDist => LocalizationService.Instance["PerformDist"];
        public string ColumnHeaderDriver => LocalizationService.Instance["Driver"];
        public string ColumnHeaderGPSArrive => LocalizationService.Instance["GPSArrive"];
        public string ColumnHeaderOdometer => LocalizationService.Instance["Odometer"];
        public string ColumnHeaderAuthNo => LocalizationService.Instance["AuthNo"];
        public string ColumnHeaderFundingSource => LocalizationService.Instance["FundingSource"];

        // Actions
        public string UnscheduleToolTip => LocalizationService.Instance["Unschedule"];
        public string SelectFieldsToDisplayToolTip => LocalizationService.Instance["SelectFieldsToDisplay"];

        // Unscheduled Trips
        public string ColumnHeaderDate => LocalizationService.Instance["Date"];
        public string ColumnHeaderFromTime => LocalizationService.Instance["FromTime"];
        public string ColumnHeaderToTime => LocalizationService.Instance["ToTime"];
        public string ColumnHeaderNotificationStatus => LocalizationService.Instance["NotificationStatus"];
        public string ColumnHeaderPatient => LocalizationService.Instance["Patient"]; // Customer
        public string ColumnHeaderPickupAddress => LocalizationService.Instance["PickupAddress"];
        public string ColumnHeaderDropoffAddress => LocalizationService.Instance["DropoffAddress"];
        public string ColumnHeaderCharge => LocalizationService.Instance["Charge"];
        public string ColumnHeaderPaid => LocalizationService.Instance["Paid"];
        //public string ColumnHeaderSpace => LocalizationService.Instance["Space"];
        public string ColumnHeaderPickupComment => LocalizationService.Instance["PickupComment"];
        public string ColumnHeaderDropoffComment => LocalizationService.Instance["DropoffComment"];
        public string ColumnHeaderType => LocalizationService.Instance["Type"];
        //public string ColumnHeaderPickup => LocalizationService.Instance["Pickup"];
        public string ColumnHeaderDropoff => LocalizationService.Instance["Dropoff"];
        public string ColumnHeaderPickupPhone => LocalizationService.Instance["PickupPhone"];
        public string ColumnHeaderDropoffPhone => LocalizationService.Instance["DropoffPhone"];
        public string ColumnHeaderAuthorization => LocalizationService.Instance["Authorization"];
        //public string ColumnHeaderFundingSource => LocalizationService.Instance["FundingSource"];
        //public string ColumnHeaderDistance => LocalizationService.Instance["Distance"];
        public string ColumnHeaderPickupCity => LocalizationService.Instance["PickupCity"];
        public string ColumnHeaderDropoffCity => LocalizationService.Instance["DropoffCity"];

        // Actions
        public string CancelTripToolTip => LocalizationService.Instance["CancelTrip"];
        public string EditTripToolTip => LocalizationService.Instance["Edit"]; 
        public string ScheduleTripToolTip => LocalizationService.Instance["ScheduleTrip"];


        // MSSG
        public string ErrorTitle => LocalizationService.Instance["ErrorTitle"];
        public string TripRoutingError => LocalizationService.Instance["TripRoutingError"]; // Trip routing error
        public string CancelScheduleError => LocalizationService.Instance["CancelScheduleError"]; // Error canceling Schedule

        #endregion
    }
}
