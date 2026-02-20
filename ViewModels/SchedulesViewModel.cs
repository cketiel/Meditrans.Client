using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2016.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using GongSolutions.Wpf.DragDrop;
using Meditrans.Client.DTOs;
using Meditrans.Client.Models;
using Meditrans.Client.Services;
using Meditrans.Client.Views.Dispatch;
using Meditrans.Client.Views.Schedules;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;


namespace Meditrans.Client.ViewModels
{
    public partial class SchedulesViewModel : ObservableObject, IDragSource, IDropTarget
    {      
        // Flag to prevent concurrent recalculations.
        private bool _isRecalculating = false;
        /// <summary>
        /// Stores the sequence of the last 'Performed' event that triggered a recalculation.
        /// It acts as a "seal" to prevent redundant recalculations.
        /// It is initialized to -1 to indicate that it has never been calculated.
        /// </summary>
        private int _lastRecalculatedSequence = -1;


        private readonly GpsService _gpsService; 
        private DispatcherTimer _liveUpdateTimer;
        private List<ScheduleDto> _masterSchedules = new List<ScheduleDto>();

        [ObservableProperty]
        private bool _allowUnperformAction = false;

        [ObservableProperty]
        private bool _showFilterControls = false; 

        [ObservableProperty]
        private bool _displayPerformedEvents = true; //By default, the checkbox will be checked.

        [ObservableProperty]
        private GpsDataDto _driverLastKnownLocation;

        [ObservableProperty]
        private bool _isLiveTrackingMode = false;

       

        [ObservableProperty]
        private GpsDataDto _liveGpsData;

        public bool IsInitialized { get; private set; } = false;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _routeSummaryText;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _busyMessage;

        public event EventHandler<ZoomAndCenterEventArgs> ZoomAndCenterRequest;

        //private readonly UserConfigService _userConfigService;
        private readonly ScheduleService _scheduleService;
        private readonly TripService _tripService;
        private readonly GoogleMapsService _googleMapsService;

        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Today.AddDays(1);

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

        public IAsyncRelayCommand ManualRefreshCommand { get; }
        public IAsyncRelayCommand UnperformEventCommand { get; }

        public IAsyncRelayCommand ShowHistoryCommand { get; }

        public SchedulesViewModel(ScheduleService scheduleService)
        {
            AllowUnperformAction = true;

            //UserConfigService _userConfigService = new UserConfigService();
            _scheduleService = scheduleService;
            _tripService = new TripService();
            _googleMapsService = new GoogleMapsService();
            _gpsService = new GpsService();

            LoadInitialDataCommand = new AsyncRelayCommand(LoadInitialDataAsync);
            LoadSchedulesAndTripsCommand = new AsyncRelayCommand(LoadSchedulesAndTripsAsync, CanLoadSchedulesAndTrips);
            RouteTripCommand = new AsyncRelayCommand(RouteSelectedTripAsync, CanRouteSelectedTrip);
            CancelRouteCommand = new AsyncRelayCommand<ScheduleDto>(CancelSelectedRouteAsync/*, CanCancelSelectedRoute*/);
            OpenColumnSelectorCommand = new RelayCommand(OpenColumnSelector);

            CancelTripCommand = new AsyncRelayCommand<object>(ExecuteCancelTripAsync);
            UncancelTripCommand = new AsyncRelayCommand<object>(ExecuteUncancelTripAsync);
            EditTripCommand = new AsyncRelayCommand<object>(ExecuteEditTripAsync);

            UnperformEventCommand = new AsyncRelayCommand<ScheduleDto>(ExecuteUnperformEventAsync);

            ManualRefreshCommand = new AsyncRelayCommand(ManualRefreshAsync); // RefreshLiveDataAsync

            ShowHistoryCommand = new AsyncRelayCommand<object>(ExecuteShowHistoryAsync);

            InitializeColumns();
            //_ = InitializeAsync();
         
        }

        private async Task ExecuteShowHistoryAsync(object parameter)
        {
            TripReadDto tripToView = null;

            if (parameter is UnscheduledTripDto unscheduled)
            {               
                tripToView = new TripReadDto
                {
                    Id = unscheduled.Id,
                    CustomerName = unscheduled.CustomerName,
                    PickupAddress = unscheduled.PickupAddress,
                    DropoffAddress = unscheduled.DropoffAddress
                };
            }
            else if (parameter is ScheduleDto schedule && schedule.TripId.HasValue)
            {
                
                IsBusy = true;
                try
                {
                    
                    tripToView = new TripReadDto
                    {
                        Id = schedule.TripId.Value,
                        CustomerName = schedule.Patient,
                        PickupAddress = "See trip details...", 
                        DropoffAddress = "See trip details..."
                    };
                }
                finally { IsBusy = false; }
            }

            if (tripToView == null) return;

            var viewModel = new TripHistoryViewModel(tripToView);
            var view = new Views.TripHistoryDialog { DataContext = viewModel };
           
            await MaterialDesignThemes.Wpf.DialogHost.Show(view, "RootDialogHost");
        }

        private async Task ExecuteUnperformEventAsync(ScheduleDto schedule)
        {
            if (schedule == null) return;

            // Show a confirmation dialog
            var result = MessageBox.Show(
                "Are you sure you want to undo the performed status for this event?",
                "Confirm Un-perform",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No) return;

            try
            {
                // Apply undo logic
                schedule.Arrive = null;
                schedule.ArriveDist = null;
                schedule.GPSArrive = null;
                schedule.Perform = null;
                schedule.PerformDist = null;
                schedule.Performed = false;
              
                await _scheduleService.UpdateAsync(schedule.Id, schedule);
               
                int eventIndex = Schedules.IndexOf(schedule);
                if (eventIndex >= 0)
                {
                    
                    await LoadSchedulesAndTripsAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error undoing the event status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public IAsyncRelayCommand LoadInitialDataCommand { get; }
        public IAsyncRelayCommand LoadSchedulesAndTripsCommand { get; }
        public IAsyncRelayCommand RouteTripCommand { get; }
        public IAsyncRelayCommand<ScheduleDto> CancelRouteCommand { get; }
        public ICommand OpenColumnSelectorCommand { get; }
        public IAsyncRelayCommand CancelTripCommand { get; }
        public IAsyncRelayCommand UncancelTripCommand { get; }
        public IAsyncRelayCommand EditTripCommand { get; }

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

        public async Task InitializeAsync(DateTime? date = null, VehicleRoute route = null, bool isLiveTracking = false)
        {
            if (IsInitialized) return;

            IsLoading = true;

            try
            {
                await LoadInitialDataListsAsync();

                if (date.HasValue)
                {
                    //SelectedDate = date.Value;
                    _selectedDate = date.Value; 
                    OnPropertyChanged(nameof(SelectedDate)); 
                }

                if (route != null)
                {                   
                    _selectedVehicleRoute = VehicleRoutes.FirstOrDefault(r => r.Id == route.Id) ?? VehicleRoutes.FirstOrDefault();
                    OnPropertyChanged(nameof(SelectedVehicleRoute)); 
                }
                else if (VehicleRoutes.Any() && SelectedVehicleRoute == null)
                {
                    _selectedVehicleRoute = VehicleRoutes.FirstOrDefault();
                    OnPropertyChanged(nameof(SelectedVehicleRoute));
                }

                if (CanLoadSchedulesAndTrips())
                {
                    await LoadSchedulesAndTripsAsync();
                }

                // After the initial loading, we check if a recalculation is needed.
                await CheckForPendingRecalculation();

                if (isLiveTracking)
                {
                    IsLiveTrackingMode = true;

                    StartLiveTracking();

                    /*try
                    {
                        
                        DriverLastKnownLocation = await _gpsService.GetLatestGpsDataAsync(_selectedVehicleRoute.Id);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al obtener la ubicación inicial del conductor: {ex.Message}");
                        
                    }*/                   
                }

                IsInitialized = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fatal durante la inicialización: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnDisplayPerformedEventsChanged(bool value)
        {
            FilterSchedules();
        }

        /*private void FilterSchedules()
        {
            Schedules.Clear();
            IEnumerable<ScheduleDto> filteredEvents = _masterSchedules;

            // Si el checkbox NO está marcado, filtramos los eventos que ya fueron realizados (Performed = true)
            if (!DisplayPerformedEvents)
            {
                filteredEvents = filteredEvents.Where(s => !s.Performed);
            }

            foreach (var schedule in filteredEvents)
            {
                Schedules.Add(schedule);
            }

            // Opcional: Si quieres re-calcular la secuencia visual después de filtrar
            for (int i = 0; i < Schedules.Count; i++)
            {
                Schedules[i].Sequence = i;
            }
        }*/
      
        private void FilterSchedules()
        {
            Schedules.Clear();
            IEnumerable<ScheduleDto> filteredEvents = _masterSchedules;

            // To display the sequence correctly taking into account canceled trips since only the Pickup event is displayed
            for (int i = 0; i < _masterSchedules.Count; i++)
            {
                _masterSchedules[i].Sequence = i;
                
            }

            if (!DisplayPerformedEvents)
            {
                filteredEvents = filteredEvents.Where(s => !s.Performed);
            }

            foreach (var schedule in filteredEvents)
            {
                Schedules.Add(schedule);
            }

            CalculateVisualOffsets();
        }

        private void StartLiveTracking()
        {
            _liveUpdateTimer = new DispatcherTimer
            {
                // Defines the update interval.
                Interval = TimeSpan.FromSeconds(3) // Update every 5 seconds
            };
            // Subscribe the Tick event to our update method.
            _liveUpdateTimer.Tick += async (s, e) => await RefreshLiveDataAsync();
            // Start the timer.
            _liveUpdateTimer.Start();

            // Make an immediate first call so as not to wait 5 seconds
            _ = RefreshLiveDataAsync();
        }

        // It is forced to update, refresh the map and grid data
        private async Task ManualRefreshAsync()
        {
            await RefreshLiveDataAsync(); // refresh map
            await LoadSchedulesAndTripsAsync(); // refresh grids
        }

        private async Task RefreshLiveDataAsync()
        {
            // Additional security measure: if a recalculation is already in progress,
            // We skip this refresh cycle so as not to interfere.
            if (_isRecalculating) return;

            if (SelectedVehicleRoute == null) return;

            try
            {
                var gpsData = await _gpsService.GetLatestGpsDataAsync(SelectedVehicleRoute.Id);
                if (gpsData != null)
                {
                    // CommunityToolkit notifies the UI, and the marker moves.
                    DriverLastKnownLocation = gpsData;
                }

                var latestSchedules = await _scheduleService.GetSchedulesAsync(SelectedVehicleRoute.Id, SelectedDate);

                // Only events that changed will be updated
                bool stateChanged = MergeScheduleUpdates(latestSchedules);

                // If the status of an event changed to 'Performed', we need to recalculate.
                /*if (stateChanged)
                {
                    // We look up the index of the latest event which is now marked 'Performed'.
                    var lastPerformedIndex = Schedules
                        .Select((schedule, index) => new { schedule, index })
                        .Where(x => x.schedule.Performed)
                        .OrderByDescending(x => x.schedule.Sequence)
                        .Select(x => (int?)x.index)
                        .FirstOrDefault();

                    // If we find an event and it is not the last one in the list, we recalculate from the next one.
                    if (lastPerformedIndex.HasValue && lastPerformedIndex.Value < Schedules.Count - 1)
                    {
                        // We call our method recalculation!
                        await RecalculateScheduleAsync(lastPerformedIndex.Value + 1);
                    }
                }*/

                await CheckForPendingRecalculation();

                CalculateVisualOffsets();
                UpdateRouteSummary();
                //await LoadSchedulesAndTripsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching live GPS data: {ex.Message}");
                
            }
        }

        /// <summary>
        /// Checks if a new event has completed since the last recalculation, and
        /// If so, a new ETA recalculation begins.
        /// </summary>
        private async Task CheckForPendingRecalculation()
        {
            // We look for the last event that is marked as 'Performed'.
            var lastPerformedEvent = Schedules
                .Where(s => s.Performed && s.Sequence.HasValue)
                .OrderByDescending(s => s.Sequence.Value)
                .FirstOrDefault();

            // If there is no event held, there is nothing to do.
            if (lastPerformedEvent == null) return;

            // If the sequence of the last completed event is GREATER than our "stamp",
            // It means there has been progress on the route and we need to recalculate.
            if (lastPerformedEvent.Sequence.Value > _lastRecalculatedSequence)
            {
                // The start index is that of the event NEXT to the one that just completed.
                int startIndex = lastPerformedEvent.Sequence.Value + 1;

                // We recalculate
                await RecalculateScheduleAsync(startIndex);

                // IMPORTANT! We updated our "seal" to not recalculate for this same event.
                _lastRecalculatedSequence = lastPerformedEvent.Sequence.Value;
            }
        }

        /// <summary>
        /// Updates the existing 'Schedules' collection with data from a new list,
        /// modifying properties instead of replacing objects. This preserves the UI selection.
        /// </summary>
        /// <param name="latestSchedules">The list of schedules just obtained from the API.</param>
        private bool MergeScheduleUpdates(List<ScheduleDto> latestSchedules)
        {
            bool performanceStateChanged = false; // Flag to track whether an event completed.

            var existingSchedulesDict = Schedules.ToDictionary(s => s.Id);

            foreach (var latestSchedule in latestSchedules)
            {
                // We look to see if the newly obtained schedule already exists in our visible collection.
                if (existingSchedulesDict.TryGetValue(latestSchedule.Id, out var existingSchedule))
                {
                    // We detect if the 'Performed' state changed from false to true.
                    if (!existingSchedule.Performed && latestSchedule.Performed)
                    {
                        performanceStateChanged = true;
                    }

                    // If it exists, we update its properties.
                    // Since ScheduleDto is an ObservableObject, the UI will react to every change.
                    existingSchedule.ETA = latestSchedule.ETA;
                    existingSchedule.Arrive = latestSchedule.Arrive;
                    existingSchedule.Perform = latestSchedule.Perform;
                    existingSchedule.Performed = latestSchedule.Performed;
                    existingSchedule.ArriveDist = latestSchedule.ArriveDist;
                    existingSchedule.PerformDist = latestSchedule.PerformDist;
                    existingSchedule.GPSArrive = latestSchedule.GPSArrive;
                    existingSchedule.Status = latestSchedule.Status;                  
                }
                // Note: This simple implementation does not handle schedules that are added or removed

            }
            return performanceStateChanged;
        }

        public void Cleanup()
        {
            // Stops the timer and frees resources to prevent memory leaks.
            _liveUpdateTimer?.Stop();
            if (_liveUpdateTimer != null)
            {
                _liveUpdateTimer.Tick -= async (s, e) => await RefreshLiveDataAsync();
            }
            _liveUpdateTimer = null;
        }
        
        // Load initial data (route and group lists)

        private async Task LoadInitialDataListsAsync()
        {
            // Este método solo carga las listas de ComboBox, sin seleccionar nada.
            RunService _runService = new RunService();
            var routes = await _runService.GetAllAsync();
            VehicleRoutes.Clear();
            foreach (var r in routes)
            {
                var now = DateTime.UtcNow;
                bool inDateRange = now >= r.FromDate && (r.ToDate == null || now <= r.ToDate);
                bool isSuspended = r.Suspensions.Any(s => now >= s.SuspensionStart && now <= s.SuspensionEnd);
                if (inDateRange && !isSuspended)
                    VehicleRoutes.Add(r);
            }

            VehicleGroupService _vehicleGroupService = new VehicleGroupService();
            var groups = await _vehicleGroupService.GetGroupsAsync();
            VehicleGroups.Clear();
            foreach (var g in groups)
            {
                VehicleGroups.Add(g);
            }
        }

        private async Task LoadInitialDataAsync()
        {
            RunService _runService = new RunService();
            var routes = await _runService.GetAllAsync();

            VehicleRoutes.Clear();
            foreach (var route in routes)
            {
                var now = DateTime.UtcNow;
                bool inDateRange = now >= route.FromDate && (route.ToDate == null || now <= route.ToDate);
                bool isSuspended = route.Suspensions.Any(s => now >= s.SuspensionStart && now <= s.SuspensionEnd);
                bool isActive = inDateRange && !isSuspended;
                if (isActive == true)
                    VehicleRoutes.Add(route);
            }

            if (VehicleRoutes.Any() && this.SelectedVehicleRoute == null)
            {
                SelectedVehicleRoute = VehicleRoutes[0];
            }

            VehicleGroupService _vehicleGroupService = new VehicleGroupService();
            var groups = await _vehicleGroupService.GetGroupsAsync();
            VehicleGroups.Clear();
            foreach (var group in groups)
            {
                VehicleGroups.Add(group);
            }
        }

        public async Task LoadDataAsync()
        {          
            if (LoadSchedulesAndTripsCommand.CanExecute(null))
            {
                await LoadSchedulesAndTripsCommand.ExecuteAsync(null);
            }
        }

        // Load the main grids
        private async Task LoadSchedulesAndTripsAsync()
        {
            IsLoading = true;

            try
            {
                _masterSchedules.Clear();
                //Schedules.Clear();
                UnscheduledTrips.Clear();
                SelectedUnscheduledTripPoints.Clear();

                var schedules = await _scheduleService.GetSchedulesAsync(SelectedVehicleRoute.Id, SelectedDate);
                _masterSchedules.AddRange(schedules);
                // To display the sequence correctly taking into account canceled trips since only the Pickup event is displayed
                /*for (int i = 0; i < schedules.Count; i++)
                {
                    schedules[i].Sequence = i;
                    Schedules.Add(schedules[i]);
                }*/

                FilterSchedules();

                //foreach (var s in schedules) Schedules.Add(s);

                var trips = await _scheduleService.GetUnscheduledTripsAsync(SelectedDate);

                //var geocodingTasks = trips.Select(trip => PopulateCitiesForTravel(trip)).ToList();
                //await Task.WhenAll(geocodingTasks);

                // Only consume the Google Maps service if the Trip object does not have PickupCity or DropoffCity
                foreach (var source in trips)
                {
                    /*if (source.PickupCity.Equals("") || source.PickupCity == null)
                        source.PickupCity = await _googleMapsService.GetCityFromCoordinates(source.PickupLatitude, source.PickupLongitude) ?? "N/A";
                    if (source.DropoffCity.Equals("") || source.DropoffCity == null)
                        source.DropoffCity = await _googleMapsService.GetCityFromCoordinates(source.DropoffLatitude, source.DropoffLongitude) ?? "N/A";*/
                    UnscheduledTrips.Add(source);
                }
                //foreach (var t in trips) UnscheduledTrips.Add(t);

                UpdateMapViewForAllPoints();
                UpdateRouteSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading schedule data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {              
                IsLoading = false;
            }

        }
        public void TriggerZoomToFit()
        {
            UpdateMapViewForAllPoints();

            
        }


        private async Task PopulateCitiesForTravel(UnscheduledTripDto trip)
        {
            // Get the city of origin (Pickup)
            trip.PickupCity = await _googleMapsService.GetCityFromCoordinates(
                trip.PickupLatitude,
                trip.PickupLongitude) ?? "N/A"; // "N/A" = Not Available

            // Get the destination city (Dropoff)
            trip.DropoffCity = await _googleMapsService.GetCityFromCoordinates(
                trip.DropoffLatitude,
                trip.DropoffLongitude) ?? "N/A";
        }

        private bool CanLoadSchedulesAndTrips() => SelectedVehicleRoute != null;

        // Routing logic
        private async Task RouteSelectedTripAsync()
        {
            var tripToSchedule = SelectedUnscheduledTrip;
            var vehicleRoute = SelectedVehicleRoute;

            if (tripToSchedule == null || vehicleRoute == null)
            {
                MessageBox.Show("Please select a trip and a vehicle route before routing.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IsBusy = true;
            BusyMessage = "Calculating optimal route...";

            try
            {
                
                var previousSchedule = Schedules.LastOrDefault(s => s.ETA < tripToSchedule.FromTime && s.Name != "Pull-in");
                double originLat, originLng;
                TimeSpan previousEta, previousServiceTime;

                if (previousSchedule == null || previousSchedule.Name == "Pull-out")
                {
                    originLat = vehicleRoute.GarageLatitude;
                    originLng = vehicleRoute.GarageLongitude;
                    previousEta = Schedules.FirstOrDefault(s => s.Name == "Pull-out")?.ETA ?? (tripToSchedule.FromTime ?? TimeSpan.Zero) - TimeSpan.FromMinutes(30);
                    previousServiceTime = TimeSpan.Zero;
                }
                else
                {
                    originLat = previousSchedule.ScheduleLatitude;
                    originLng = previousSchedule.ScheduleLongitude;
                    previousEta = previousSchedule.ETA ?? TimeSpan.Zero;
                    previousServiceTime = TimeSpan.FromMinutes(previousSchedule.On ?? 15);
                }

                var pickupDetails = await _googleMapsService.GetRouteFullDetails(originLat, originLng, tripToSchedule.PickupLatitude, tripToSchedule.PickupLongitude);
                if (pickupDetails == null) throw new Exception("Could not calculate the route to the pickup point.");

                double pDistance = pickupDetails.DistanceMiles;
                TimeSpan pTravelTime = TimeSpan.FromSeconds(pickupDetails.DurationInTrafficSeconds);
                TimeSpan pCalculatedEta = previousEta + previousServiceTime + pTravelTime;
                TimeSpan pFinalEta = pCalculatedEta;
                if (previousSchedule == null || previousSchedule.EventType != ScheduleEventType.Pickup)
                {
                    TimeSpan pViolationLimit = (tripToSchedule.FromTime ?? TimeSpan.Zero) - TimeSpan.FromMinutes(15);
                    if (pCalculatedEta < pViolationLimit) pFinalEta = pViolationLimit;
                }

                var dropoffDetails = await _googleMapsService.GetRouteFullDetails(tripToSchedule.PickupLatitude, tripToSchedule.PickupLongitude, tripToSchedule.DropoffLatitude, tripToSchedule.DropoffLongitude);
                if (dropoffDetails == null) throw new Exception("Could not calculate the route to the dropoff point.");

                double dDistance = dropoffDetails.DistanceMiles;
                TimeSpan dTravelTime = TimeSpan.FromSeconds(dropoffDetails.DurationInTrafficSeconds);
                TimeSpan pickupServiceTime = TimeSpan.FromMinutes(15);
                TimeSpan dFinalEta = pFinalEta + pickupServiceTime + dTravelTime;

                var request = new RouteTripRequest
                {
                    VehicleRouteId = vehicleRoute.Id,
                    TripId = tripToSchedule.Id,
                    PickupDistance = pDistance,
                    PickupTravelTime = pTravelTime,
                    PickupETA = pFinalEta,
                    DropoffDistance = dDistance,
                    DropoffTravelTime = dTravelTime,
                    DropoffETA = dFinalEta,
                    VehicleRouteName = vehicleRoute.Name
                };

                await _scheduleService.RouteTripsAsync(request);
              

                BusyMessage = "Route updated. Finalizing calculations...";

                // We load the list, which may come with the wrong order from the backend.
                await LoadSchedulesAndTripsAsync();

                // Before recalculating, we make sure that the "Pull-in" is at the end of the in-memory collection.
                var pullInEvent = Schedules.FirstOrDefault(s => s.Name == "Pull-in");
                if (pullInEvent != null)
                {
                    int pullInIndex = Schedules.IndexOf(pullInEvent);
                    if (pullInIndex != Schedules.Count - 1)
                    {
                        // If the Pull-in is not in the last position, we move it there.
                        // This fixes the backend sort error in our local view.
                        Schedules.Move(pullInIndex, Schedules.Count - 1);
                    }
                }

                // Now that the collection is in the correct order, we proceed with the recalculation.
                var newPickupEvent = Schedules.FirstOrDefault(s => s.TripId == tripToSchedule.Id && s.EventType == ScheduleEventType.Pickup);
                if (newPickupEvent != null)
                {
                    int startIndex = Schedules.IndexOf(newPickupEvent);
                    // We pass the already corrected list to the recalculation method.
                    await RecalculateScheduleAsync(startIndex);
                }

                // We reload to ensure that the UI reflects the final state saved in the DB.
                await LoadSchedulesAndTripsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Trip routing error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                await LoadSchedulesAndTripsAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanRouteSelectedTrip() => SelectedVehicleRoute != null && SelectedUnscheduledTrip != null;

        // Logic to cancel
        private async Task CancelSelectedRouteAsync(ScheduleDto schedule)
        {
            if (schedule == null) return;

            try
            {
                await _scheduleService.CancelRouteAsync(schedule.Id);

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
            // Only reload if the VM has already been fully initialized.
            if (IsInitialized && CanLoadSchedulesAndTrips())
                _ = LoadSchedulesAndTripsAsync();

            /*if (CanLoadSchedulesAndTrips())
                LoadSchedulesAndTripsCommand.Execute(null);*/
        }

        partial void OnSelectedVehicleRouteChanged(VehicleRoute value)
        {
            // Only reload if the VM has already been fully initialized.
            if (IsInitialized && CanLoadSchedulesAndTrips())
                _ = LoadSchedulesAndTripsAsync();


            /*if (CanLoadSchedulesAndTrips())
                LoadSchedulesAndTripsCommand.Execute(null);*/
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

            // 3. If we have the driver's location, we add it to the list for the zoom calculation.
            if (DriverLastKnownLocation != null)
            {
                allPoints.Add(new PointLatLng(DriverLastKnownLocation.Latitude, DriverLastKnownLocation.Longitude));
            }

            // 4. Call the existing method that calculates the rectangle and raises the event
            ZoomAndCenterOnPoints(allPoints);
        }

        public void ForceRefreshSchedules()
        {
            var items = new List<ScheduleDto>(Schedules);
            Schedules.Clear();
            foreach (var item in items)
            {
                Schedules.Add(item);
            }
          
            FilterSchedules();
        }

        private void CalculateVisualOffsets()
        {         
            foreach (var schedule in Schedules)
            {
                schedule.VisualOffsetIndex = 0;
            }


            // We group events by their coordinates and filter out only groups with more than one member (overlaps).
            var overlappingGroups = Schedules
                .GroupBy(s => (s.ScheduleLatitude, s.ScheduleLongitude))
                .Where(g => g.Count() > 1);

            foreach (var group in overlappingGroups)
            {
                int index = 0;
                // We assign an incremental index to each event within the group.
                // Sorting by sequence ensures that scrolling is consistent.
                foreach (var scheduleInGroup in group.OrderBy(s => s.Sequence))
                {
                    scheduleInGroup.VisualOffsetIndex = index;
                    index++;
                }
            }
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
                UpdateRouteSummary();
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
            // If we are already recalculating, we leave to avoid problems.
            if (_isRecalculating) return;

            // If the collection is empty or only has the Pull-out, there is nothing to do.
            if (Schedules.Count <= 1) return;

            // If the collection is very small or the index is invalid, there is nothing to do.
            // startIndex should point to an actual event, not the Pull-in.
            if (Schedules.Count <= 2 || startIndex <= 0 || startIndex >= Schedules.Count - 1) return;

            try
            {
                _isRecalculating = true;
                IsBusy = true; 
                BusyMessage = "Recalculating route ETAs...";

                for (int i = startIndex; i < Schedules.Count - 1; i++)
                {
                    var currentSchedule = Schedules[i];
                    ScheduleDto previousSchedule = Schedules[i - 1];

                    // If the current event is already 'Performed', we do not need to recalculate its ETA.
                    // We just skip to the next one.
                    if (currentSchedule.Performed)
                    {
                        continue;
                    }

                    currentSchedule.Sequence = i;
              
                    var routeDetails = await _googleMapsService.GetRouteFullDetails(
                        previousSchedule.ScheduleLatitude,
                        previousSchedule.ScheduleLongitude,
                        currentSchedule.ScheduleLatitude,
                        currentSchedule.ScheduleLongitude);

                    if (routeDetails != null)
                    {
                        currentSchedule.Distance = routeDetails.DistanceMiles;
                        currentSchedule.Travel = TimeSpan.FromSeconds(routeDetails.DurationInTrafficSeconds);
                    }
                    else
                    {
                        currentSchedule.Distance = 0;
                        currentSchedule.Travel = TimeSpan.Zero;
                    }

                    TimeSpan travelToCurrent = currentSchedule.Travel ?? TimeSpan.Zero;

                    if (previousSchedule.Name.Equals("Pull-out")) // Update Pull-out ETA based on the first real stop.
                    {
                        // ETATime = tripToRoute.FromTime - (TimeSpan.FromMinutes(20) + request.PickupTravelTime)
                        previousSchedule.ETA = currentSchedule.Pickup - (TimeSpan.FromMinutes(20) + travelToCurrent);
                        await _scheduleService.UpdateAsync(previousSchedule.Id, previousSchedule);
                    }

                    TimeSpan previousEta = previousSchedule.ETA ?? TimeSpan.Zero;
                    TimeSpan previousServiceTime = TimeSpan.FromMinutes(previousSchedule.On ?? 15);
                    
                    TimeSpan calculatedEta = previousEta + previousServiceTime + travelToCurrent;
              
                    TimeSpan finalEta = calculatedEta;
                    if (currentSchedule.EventType == ScheduleEventType.Pickup && previousSchedule.EventType != ScheduleEventType.Pickup)
                    {
                        TimeSpan? scheduledTime = currentSchedule.Pickup;
                        if (scheduledTime.HasValue)
                        {
                            TimeSpan earlyArrivalWindow = (currentSchedule.TripType == "Return")
                                ? TimeSpan.FromMinutes(5)
                                : TimeSpan.FromMinutes(15);
                            TimeSpan violationLimit = scheduledTime.Value - earlyArrivalWindow;
                            if (calculatedEta < violationLimit)
                            {
                                finalEta = violationLimit;
                            }
                        }
                    }
               
                    currentSchedule.ETA = finalEta;
                    await _scheduleService.UpdateAsync(currentSchedule.Id, currentSchedule);
                }
           
                if (Schedules.Count > 1)
                {
                    var pullInEvent = Schedules.Last();
                    var lastRealStop = Schedules[Schedules.Count - 2]; // The last event BEFORE the Pull-in
              
                    pullInEvent.Sequence = Schedules.Count - 1;
              
                    var finalRouteDetails = await _googleMapsService.GetRouteFullDetails(
                        lastRealStop.ScheduleLatitude, lastRealStop.ScheduleLongitude,
                        pullInEvent.ScheduleLatitude, pullInEvent.ScheduleLongitude);

                    if (finalRouteDetails != null)
                    {
                        pullInEvent.Distance = finalRouteDetails.DistanceMiles;
                        pullInEvent.Travel = TimeSpan.FromSeconds(finalRouteDetails.DurationInTrafficSeconds);
                    }
                    else
                    {
                        pullInEvent.Distance = 0;
                        pullInEvent.Travel = TimeSpan.Zero;
                    }
               
                    TimeSpan lastStopEta = lastRealStop.ETA ?? TimeSpan.Zero;
                    TimeSpan lastStopServiceTime = TimeSpan.FromMinutes(lastRealStop.On ?? 15);
                    TimeSpan travelToPullIn = pullInEvent.Travel ?? TimeSpan.Zero;
                    pullInEvent.ETA = lastStopEta + lastStopServiceTime + travelToPullIn;
              
                    await _scheduleService.UpdateAsync(pullInEvent.Id, pullInEvent);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to recalculate schedule: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isRecalculating = false;
                IsBusy = false;
            }
        }
        public void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo)
        {
            // Called when the entire drag-drop operation has finished.
            // It is useful for cleaning if necessary.
            
        }

        #endregion

        private void UpdateRouteSummary()
        {
            if (Schedules == null || !Schedules.Any())
            {
                RouteSummaryText = string.Empty;
                return;
            }

            // Calculate the number of unique trips (ignoring Pull-in/out that do not have TripId)
            int tripCount = Schedules.Where(s => s.TripId.HasValue)
                                     .Select(s => s.TripId)
                                     .Distinct()
                                     .Count();

            // Calculate the total distance (handling possible null values ​​in Distance)
            double totalDistance = Schedules.Sum(s => s.Distance ?? 0.0);

            // Format the final text, handling the plural of "trip"
            string tripLabel = (tripCount == 1) ? "trip" : "trips";
            RouteSummaryText = $"{tripCount} {tripLabel}, estimated distance: {totalDistance:N1} miles"; // N1 formatea a 1 decimal
        }

        private async Task ExecuteCancelTripAsync(object parameter)
        {
            
            var tripToCancel = parameter as UnscheduledTripDto;
            if (tripToCancel == null) return;

            var confirmationText = $"Are you sure you want to cancel trip '{tripToCancel.Id}'?";
            var confirmationTitle = "Confirm Cancellation";

            if (MessageBox.Show(confirmationText, confirmationTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    await _tripService.CancelTripAsync(tripToCancel.Id);
                    MessageBox.Show("Trip canceled successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Reload the list so that the grid is updated with the new state
                    await LoadSchedulesAndTripsAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error canceling trip: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ExecuteUncancelTripAsync(object parameter)
        {
            var tripToUncancel = parameter as UnscheduledTripDto;
            if (tripToUncancel == null) return;

            var confirmationText = $"Are you sure you want to restore trip '{tripToUncancel.Id}'?";
            var confirmationTitle = "Confirm Restoration";

            if (MessageBox.Show(confirmationText, confirmationTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    await _tripService.UncancelTripAsync(tripToUncancel.Id);
                    MessageBox.Show("Trip restored successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                   
                    await LoadSchedulesAndTripsAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error restoring trip: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ExecuteEditTripAsync(object parameter)
        {
            var tripToEdit = parameter as UnscheduledTripDto;
            if (tripToEdit == null) return;
           
            var dialogViewModel = new EditTripDialogViewModel(tripToEdit);
            var dialog = new EditTripDialog { DataContext = dialogViewModel };
           
            var result = await MaterialDesignThemes.Wpf.DialogHost.Show(dialog, "RootDialogHost");

            if (result is bool wasSaved && wasSaved)
            {
                try
                {
                    var updatedDto = dialogViewModel.GetUpdatedDto();
                    await _tripService.UpdateFromDispatchAsync(tripToEdit.Id, updatedDto);

                    // Reload to see changes
                    await LoadSchedulesAndTripsAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating trip: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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
