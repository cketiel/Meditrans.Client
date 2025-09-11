using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Spreadsheet;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using Meditrans.Client.DTOs;
using Meditrans.Client.Models;
using Meditrans.Client.Services;
using Meditrans.Client.Views.Schedules;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GongSolutions.Wpf.DragDrop;


namespace Meditrans.Client.ViewModels
{
    public partial class SchedulesViewModel : ObservableObject, IDragSource, IDropTarget

    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _busyMessage;

        public event EventHandler<ZoomAndCenterEventArgs> ZoomAndCenterRequest;

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

        #region Map Properties      

        [ObservableProperty]
        private PointLatLng _mapCenter = new PointLatLng(26.616666666667, -81.833333333333); // Fort Myers, Florida

        [ObservableProperty]
        private int _mapZoom = 12;
      
        [ObservableProperty]
        private ObservableCollection<MapPoint> _selectedUnscheduledTripPoints = new();

        #endregion

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
            SelectedUnscheduledTripPoints.Clear();

            var schedules = await _scheduleService.GetSchedulesAsync(SelectedVehicleRoute.Id, SelectedDate);
            foreach (var s in schedules) Schedules.Add(s);

            var trips = await _scheduleService.GetUnscheduledTripsAsync(SelectedDate);
            foreach (var t in trips) UnscheduledTrips.Add(t);

            UpdateMapViewForAllPoints();

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

        // Logic to execute when the selection in the Schedules grid changes
        partial void OnSelectedScheduleChanged(ScheduleDto value)
        {          
            foreach (var schedule in Schedules)
            {
                schedule.IsSelectedForMap = false;
            }
           
            if (value != null)
            {
                //SelectedUnscheduledTrip = null; // This will clear the markers for the unscheduled trip

                value.IsSelectedForMap = true;
                var pairedEvent = Schedules.FirstOrDefault(s => s.TripId == value.TripId && s.Id != value.Id);
                if (pairedEvent != null)
                {
                    pairedEvent.IsSelectedForMap = true;
                }
            }
           
            UpdateMapViewForAllPoints();
        }

        partial void OnSelectedUnscheduledTripChanged(UnscheduledTripDto value)
        {          
            /*foreach (var schedule in Schedules)
            {
                schedule.IsSelectedForMap = false;
            }*/
            SelectedUnscheduledTripPoints.Clear();
           
            if (value != null)
            {
                //SelectedSchedule = null; // This will unhighlight the pair in the schedule grid.

                SelectedUnscheduledTripPoints.Add(new MapPoint
                {
                    Latitude = value.PickupLatitude,
                    Longitude = value.PickupLongitude,
                    Type = "Pickup"
                });
                SelectedUnscheduledTripPoints.Add(new MapPoint
                {
                    Latitude = value.DropoffLatitude,
                    Longitude = value.DropoffLongitude,
                    Type = "Dropoff"
                });
            }
          
            UpdateMapViewForAllPoints();
        }


        private void ZoomAndCenterOnPoints(List<PointLatLng> points)
        {
            if (points == null || !points.Any()) return;

            if (points.Count == 1)
            {
                // If there is only one point, we simply center with a fixed zoom.
                MapCenter = points.First();
                MapZoom = 14;
            }
            else
            {
                // If there are multiple points, we calculate the rectangle and fire the event.
                double maxLat = points.Max(p => p.Lat);
                double minLat = points.Min(p => p.Lat);
                double maxLng = points.Max(p => p.Lng);
                double minLng = points.Min(p => p.Lng);

                // We create the RectLatLng with the min/max coordinates
                var rect = new RectLatLng(maxLat, minLng, Math.Abs(maxLng - minLng), Math.Abs(maxLat - minLat));

                // We add a small margin (padding) so that it is not right on the edge.
                rect.Inflate(0.1, 0.1);

                // We throw the event for the View to handle.
                ZoomAndCenterRequest?.Invoke(this, new ZoomAndCenterEventArgs(rect));
            }
        }

        private void UpdateMapViewForAllPoints()
        {
            var allPoints = new List<PointLatLng>();

            // 1. Add all points of the scheduled route (Schedules)
            if (Schedules != null)
            {
                allPoints.AddRange(Schedules.Select(s => new PointLatLng(s.ScheduleLatitude, s.ScheduleLongitude)));
            }

            // 2. If there is an unscheduled trip selected, also add your points
            if (SelectedUnscheduledTrip != null)
            {
                allPoints.Add(new PointLatLng(SelectedUnscheduledTrip.PickupLatitude, SelectedUnscheduledTrip.PickupLongitude));
                allPoints.Add(new PointLatLng(SelectedUnscheduledTrip.DropoffLatitude, SelectedUnscheduledTrip.DropoffLongitude));
            }

            // 3. Call the existing method that calculates the rectangle and raises the event
            ZoomAndCenterOnPoints(allPoints);
        }


        #region Drag and Drop Implementation

        // --- IDragSource: Controls the start of the drag ---

        public void StartDrag(IDragInfo dragInfo)
        {
            
        }

        public bool CanStartDrag(IDragInfo dragInfo)
        {
            // Validation to NOT allow "Pull-out" or "Pull-in" dragging.
            if (dragInfo.SourceItem is ScheduleDto schedule)
            {
                return schedule.Name != "Pull-out" && schedule.Name != "Pull-in";
            }
            return false;
        }

        public void Dropped(IDropInfo dropInfo)
        {
            // This is called after the drop operation has been completed
           
        }

        public void DragCancelled()
        {
            // Called if the drag is canceled (e.g. by pressing ESC).
        }

        public bool TryCatchOccurredException(Exception exception)
        {
            // Allows you to handle exceptions that may occur during the drag-drop.
            MessageBox.Show($"An error occurred during drag and drop: {exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return true; // true to indicate that the exception has been handled.
        }


        // --- IDropTarget: Control the validation and execution of the drop ---

        public void DragOver(IDropInfo dropInfo)
        {
            var sourceItem = dropInfo.Data as ScheduleDto;
            var targetItem = dropInfo.TargetItem as ScheduleDto;

            if (sourceItem == null || targetItem == null)
            {
                dropInfo.Effects = DragDropEffects.None;
                return;
            }

            // --- VALIDATIONS IN REAL TIME ---

            // 1. You cannot release it on "Pull-out" or "Pull-in".
            if (targetItem.Name == "Pull-out" || targetItem.Name == "Pull-in")
            {
                dropInfo.Effects = DragDropEffects.None;
                return;
            }

            // 2. A "Dropoff" cannot go BEFORE its corresponding "Pickup".
            if (sourceItem.EventType == ScheduleEventType.Dropoff)
            {
                // Find the Pickup for this trip
                var pickupItem = Schedules.FirstOrDefault(s => s.TripId == sourceItem.TripId && s.EventType == ScheduleEventType.Pickup);
                if (pickupItem != null)
                {
                    int pickupIndex = Schedules.IndexOf(pickupItem);
                    // If the index where you want to drop is less than or equal to that of the pickup, it is not valid.
                    if (dropInfo.InsertIndex <= pickupIndex)
                    {
                        dropInfo.Effects = DragDropEffects.None;
                        return;
                    }
                }
            }


            // 3. A "Pickup" cannot go AFTER its corresponding "Dropoff".
            if (sourceItem.EventType == ScheduleEventType.Pickup)
            {
                // Find the Dropoff for this trip
                var dropoffItem = Schedules.FirstOrDefault(s => s.TripId == sourceItem.TripId && s.EventType == ScheduleEventType.Dropoff);
                if (dropoffItem != null)
                {
                    int dropoffIndex = Schedules.IndexOf(dropoffItem);
                    // If the index where you want to drop is greater than or equal to the dropoff, it is not valid.
                    // dropInfo.InsertIndex gives us the position *before* which it will be inserted.
                    // If we move an element from a low index to a high index, the dropoff index may change.
                    // Therefore, the simple condition is the most effective.
                    if (dropInfo.InsertIndex >= dropoffIndex)
                    {
                        dropInfo.Effects = DragDropEffects.None;
                        return;
                    }
                }
            }

            // If all validations pass, we display the "Move" visual effect.
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
            dropInfo.Effects = DragDropEffects.Move;
        }

        public async void Drop(IDropInfo dropInfo)
        {
            var sourceItem = dropInfo.Data as ScheduleDto;
            if (sourceItem == null) return;

            int oldIndex = Schedules.IndexOf(sourceItem);
            int newIndex = dropInfo.InsertIndex;

            // Adjust index if item moves down list
            if (oldIndex < newIndex)
            {
                newIndex--;
            }

            //Move element in observable collection so UI updates
            Schedules.Move(oldIndex, newIndex);

            // --- RECALCULATION AND PERSISTENCE ---
            IsBusy = true;
            BusyMessage = "Recalculating and saving route...";
            try
            {
                // The first element affected is the one in the earliest position
                int startIndex = Math.Min(oldIndex, newIndex);
                await RecalculateScheduleAsync(startIndex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update the schedule: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Optional: Reload data to undo visual changes if save fails
                await LoadSchedulesAndTripsAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RecalculateScheduleAsync(int startIndex)
        {
            
            GoogleMapsService googleMapsService = new GoogleMapsService();

            // Go from the first affected element to the penultimate one (Pull-in is handled separately)
            for (int i = startIndex; i < Schedules.Count - 1; i++)
            {
                var currentSchedule = Schedules[i];
                var previousSchedule = Schedules[i - 1];

                // 1. Get coordinates of the previous and current point
                var prevCoords = new { Lat = previousSchedule.ScheduleLatitude, Lng = previousSchedule.ScheduleLongitude };
                var currCoords = new { Lat = currentSchedule.ScheduleLatitude, Lng = currentSchedule.ScheduleLongitude };

                // 2. Call Google API to get new route data
                var routeDetails = await googleMapsService.GetRouteFullDetails(prevCoords.Lat, prevCoords.Lng, currCoords.Lat, currCoords.Lng);

                // 3. Update Sequence, Distance and Travel
                currentSchedule.Sequence = i;
                if (routeDetails != null)
                {
                    currentSchedule.Distance = routeDetails.DistanceMiles;
                    currentSchedule.Travel = TimeSpan.FromSeconds(routeDetails.DurationInTrafficSeconds);
                }
                else // Handle the case where the Google API fails
                {
                    currentSchedule.Distance = 0;
                    currentSchedule.Travel = TimeSpan.Zero;
                }

                // 4. Calculate the "raw" ETA based on the previous event.
                // We use TimeSpan.Zero as the default value to avoid errors with nulls.
                TimeSpan previousEta = previousSchedule.ETA ?? TimeSpan.Zero;
                TimeSpan previousServiceTime = TimeSpan.FromMinutes(previousSchedule.On ?? 15); // Asumir 15 min si 'On' es nulo
                TimeSpan currentTravel = currentSchedule.Travel ?? TimeSpan.Zero;
                TimeSpan calculatedEta = previousEta + previousServiceTime + currentTravel;

                // 5. Determine the limit to "not arrive too early."
                TimeSpan? scheduledTime = (currentSchedule.EventType == ScheduleEventType.Pickup)
                    ? currentSchedule.Pickup
                    : currentSchedule.Appt;

                TimeSpan? earlyArrivalWindow = null;
                if (currentSchedule.TripType == "Appointment")
                {
                    earlyArrivalWindow = TimeSpan.FromMinutes(15);
                }
                else if (currentSchedule.TripType == "Return")
                {
                    earlyArrivalWindow = TimeSpan.FromMinutes(5);
                }

                // 6. Apply the rule and adjust the ETA if necessary.
                TimeSpan finalEta = calculatedEta; // By default, the final ETA is the calculated one.
                if (scheduledTime.HasValue && earlyArrivalWindow.HasValue)
                {
                    TimeSpan violationLimit = scheduledTime.Value - earlyArrivalWindow.Value;
                    if (calculatedEta < violationLimit)
                    {
                        // If the calculated ETA is earlier than the allowed limit,
                        // we adjust the ETA to be exactly the limit.
                        finalEta = violationLimit;
                    }
                }

                // 7. Assign the final ETA to the object.
                currentSchedule.ETA = finalEta;

                // 8. Save changes to the database
                await _scheduleService.UpdateAsync(currentSchedule.Id, currentSchedule);
            }

            // Finally, recalculate and update the Pull-in
            if (Schedules.Count > 1)
            {
                var pullIn = Schedules.Last();
                var lastStop = Schedules[Schedules.Count - 2]; // The last event before the Pull-in

                var lastStopCoords = new { Lat = lastStop.ScheduleLatitude, Lng = lastStop.ScheduleLongitude };
                var pullInCoords = new { Lat = pullIn.ScheduleLatitude, Lng = pullIn.ScheduleLongitude };

                var finalRouteDetails = await googleMapsService.GetRouteFullDetails(lastStopCoords.Lat, lastStopCoords.Lng, pullInCoords.Lat, pullInCoords.Lng);

                pullIn.Sequence = Schedules.Count - 1;
                if (finalRouteDetails != null)
                {
                    pullIn.Distance = finalRouteDetails.DistanceMiles;
                    pullIn.Travel = TimeSpan.FromSeconds(finalRouteDetails.DurationInTrafficSeconds);
                }
                else
                {
                    pullIn.Distance = 0;
                    pullIn.Travel = TimeSpan.Zero;
                }

                TimeSpan lastStopServiceTime = TimeSpan.FromMinutes(lastStop.On ?? 15);
                pullIn.ETA = (lastStop.ETA ?? TimeSpan.Zero) + lastStopServiceTime + (pullIn.Travel ?? TimeSpan.Zero);

                await _scheduleService.UpdateAsync(pullIn.Id, pullIn);
            }
        }
        public void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo)
        {
            // Called when the entire drag-drop operation has finished.
            // It is useful for cleaning if necessary.
            
        }

        #endregion

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
