using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using MaterialDesignThemes.Wpf;
using Meditrans.Client.Commands;
using Meditrans.Client.DTOs;
using Meditrans.Client.Models;
using Meditrans.Client.Services;
using Meditrans.Client.Views.Dispatch;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
//using CommunityToolkit.Mvvm.Input;
//using CommunityToolkit.Mvvm.ComponentModel;
//using CommunityToolkit.Mvvm.Input;

namespace Meditrans.Client.ViewModels
{
    public class DispatchViewModel : BaseViewModel
    {
        private readonly ScheduleService _scheduleService;
        private readonly TripService _tripService;
        private readonly GoogleMapsService _googleMapsService;

        private ObservableCollection<VehicleRouteViewModel> _allVehicleRoutes;

        #region Main Dispatch Properties

        private ObservableCollection<RunItemViewModel> _runs;
        public ObservableCollection<RunItemViewModel> Runs
        {
            get => _runs;
            set => SetProperty(ref _runs, value);
        }

        private ObservableCollection<TripItemViewModel> _allUnscheduledTripsFromService; // Todos los viajes no programados del servicio
        private ObservableCollection<TripItemViewModel> _unscheduledTrips;
        private UnscheduledTripDto _selectedUnscheduledTrip;
        public ObservableCollection<TripItemViewModel> UnscheduledTrips
        {
            get => _unscheduledTrips;
            set => SetProperty(ref _unscheduledTrips, value);
        }

        public UnscheduledTripDto SelectedUnscheduledTrip
        {
            get => _selectedUnscheduledTrip;
            set => SetProperty(ref _selectedUnscheduledTrip, value);
        }

        private bool _displayWillCalls;
        public bool DisplayWillCalls
        {
            get => _displayWillCalls;
            set
            {
                if (SetProperty(ref _displayWillCalls, value))
                {
                    FilterUnscheduledTrips();
                }
            }
        }

        #endregion

        #region Visibility and State Properties
        private bool _isOverviewVisible;
        public bool IsOverviewVisible
        {
            get => _isOverviewVisible;
            set => SetProperty(ref _isOverviewVisible, value);
        }

        private DateTime _currentDispatchDate;
        public DateTime CurrentDispatchDate
        {
            get => _currentDispatchDate;
            set
            {
                if (SetProperty(ref _currentDispatchDate, value))
                {
                    // When the date changes, we reload all the data
                    LoadAllDataAsync();
                }
            }
        }

        #endregion

        #region Properties View Overview

        private List<ScheduleDto> _masterAllEvents;
        private ObservableCollection<ScheduleDto> _allEvents;
        public ObservableCollection<ScheduleDto> AllEvents
        {
            get => _allEvents;
            set => SetProperty(ref _allEvents, value);
        }

        // Filters for the "All Events" grid
        private bool _showPerformedEvents;
        public bool ShowPerformedEvents { get => _showPerformedEvents; set { if (SetProperty(ref _showPerformedEvents, value)) FilterAllEvents(); } }

        private bool _showPullEvents;
        public bool ShowPullEvents { get => _showPullEvents; set { if (SetProperty(ref _showPullEvents, value)) FilterAllEvents(); } }

        private bool _showLatePickups;
        public bool ShowLatePickups { get => _showLatePickups; set { if (SetProperty(ref _showLatePickups, value)) FilterAllEvents(); } }

        // Properties for the information band
        public int TotalVehicles { get; set; } = 24;
        public int ClearVehicles { get; set; } = 11;
        public int LoadedVehicles { get; set; } = 5;
        public int EnRouteVehicles { get; set; } = 2;
        public int OnBreakVehicles { get; set; } = 0;
        public int OfflineVehicles { get; set; } = 3;

        private string _eventsSummaryText;
        public string EventsSummaryText { get => _eventsSummaryText; set => SetProperty(ref _eventsSummaryText, value); }

        #endregion


        #region Loading Indicator
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }
        #endregion

        #region Commands
        public ICommand OverviewCommand { get; private set; }
        public ICommand ExportScheduleCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand OpenColumnSelectorCommand { get; }
        public ICommand ShowDispatchMainCommand { get; private set; }
        public ICommand CancelTripCommand { get; private set; }
        public ICommand EditTripCommand { get; private set; }
        public ICommand UncancelTripCommand { get; private set; }

        #endregion

        public DispatchViewModel()
        {
            _scheduleService = new ScheduleService();
            _googleMapsService = new GoogleMapsService();
            _tripService = new TripService();

            _allVehicleRoutes = new ObservableCollection<VehicleRouteViewModel>();// new ObservableCollection<VehicleRoute>(); 

            // Dispatch Principal
            Runs = new ObservableCollection<RunItemViewModel>();
            _allUnscheduledTripsFromService = new ObservableCollection<TripItemViewModel>();
            UnscheduledTrips = new ObservableCollection<TripItemViewModel>();

            // Overview
            _masterAllEvents = new List<ScheduleDto>();
            AllEvents = new ObservableCollection<ScheduleDto>();

            CurrentDispatchDate = DateTime.Today;
            IsOverviewVisible = false;

            // Initialize commands
            OverviewCommand = new RelayCommandObject(ExecuteOverview);
            ExportScheduleCommand = new AsyncRelayCommand(ExecuteExportScheduleAsync);
            RefreshCommand = new AsyncRelayCommand(ExecuteRefreshAsync);
            ShowDispatchMainCommand = new RelayCommandObject(ExecuteShowDispatchMain);
            CancelTripCommand = new AsyncRelayCommand(ExecuteCancelTripAsync);
            EditTripCommand = new AsyncRelayCommand(ExecuteEditTripAsync);
            UncancelTripCommand = new AsyncRelayCommand(ExecuteUncancelTripAsync);

            //CancelTripCommand = new AsyncRelayCommand<TripItemViewModel>(ExecuteCancelTripAsync);
            //EditTripCommand = new AsyncRelayCommand<TripItemViewModel>(ExecuteEditTripAsync);

            /*OverviewCommand = new AsyncRelayCommand(ExecuteOverviewAsync); // Use AsyncRelayCommand for asynchronous operations
            ExportScheduleCommand = new AsyncRelayCommand(ExecuteExportScheduleAsync);
            RefreshCommand = new AsyncRelayCommand(ExecuteRefreshAsync);
            ShowDispatchMainCommand = new RelayCommand(ExecuteShowDispatchMain);*/

            // Initial data loading
            //LoadAllDataAsync();
        }

        private async Task ExecuteUncancelTripAsync(object parameter)
        {
            var tripToUncancel = parameter as TripItemViewModel;
            if (tripToUncancel == null) return;

            // Confirmación del usuario
            var confirmationText = $"Are you sure you want to restore trip '{tripToUncancel.Id}'?";
            var confirmationTitle = "Confirm Restoration";

            if (MessageBox.Show(confirmationText, confirmationTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    await _tripService.UncancelTripAsync(tripToUncancel.Id);

                    MessageBox.Show(
                        "Trip restored successfully.",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                   
                    await LoadUnscheduledTripsAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error restoring trip: {ex.Message}");
                    MessageBox.Show(
                        $"Error restoring trip: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private async Task ExecuteCancelTripAsync(object parameter)
        {
            var tripToCancel = parameter as TripItemViewModel;
            if (tripToCancel == null) return;

            var confirmationText = string.Format(LocalizationService.Instance["ConfirmCancelTripText"], tripToCancel.Id); // ej: "¿Está seguro de que desea cancelar el viaje '{0}'?"
            var confirmationTitle = LocalizationService.Instance["ConfirmCancelTitle"]; // ej: "Confirmar cancelación"

            if (MessageBox.Show(confirmationText, confirmationTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    await _tripService.CancelTripAsync(tripToCancel.Id);

                    
                    //UnscheduledTrips.Remove(tripToCancel);                   
                    //_allUnscheduledTripsFromService.Remove(tripToCancel);

                    MessageBox.Show(
                        LocalizationService.Instance["TripCanceledSuccessfully"], // ej: "Viaje cancelado correctamente."
                        SuccessTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    await LoadUnscheduledTripsAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error canceling trip: {ex.Message}");
                    MessageBox.Show(
                        string.Format(LocalizationService.Instance["ErrorCancelingTrip"], ex.Message), // ej: "Error al cancelar el viaje: {0}"
                        ErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }

            
        }

        private async Task ExecuteEditTripAsync(object parameter)
        {
            var tripToEdit = parameter as TripItemViewModel;
            if (tripToEdit == null) return;

            var dialogViewModel = new EditTripDialogViewModel(tripToEdit.TripDto);
            var dialog = new EditTripDialog { DataContext = dialogViewModel };

            var result = await DialogHost.Show(dialog, "RootDialogHost");

            // If the result is 'true' (the user pressed Save)
            if (result is bool wasSaved && wasSaved)
            {
                try
                {
                    var updatedDto = dialogViewModel.GetUpdatedDto();
                    await _tripService.UpdateFromDispatchAsync(tripToEdit.Id, updatedDto);

                    // Refresh data to see changes
                    await LoadUnscheduledTripsAsync();
                }
                catch (Exception ex)
                {

                    Console.WriteLine($"Error updating trip: {ex.Message}");
                }
            }
        }

        private async Task LoadAllDataAsync()
        {
            IsLoading = true;
            try
            {
                _allVehicleRoutes.Clear();
                RunService _runService = new RunService();
                var routes = await _runService.GetAllAsync();

                foreach (var route in routes)
                {
                    _allVehicleRoutes.Add(new VehicleRouteViewModel(route));
                }

                // We use Task.WhenAll to run both loads in parallel and improve performance
                await Task.WhenAll(
                    LoadRunsAndEventsAsync(),
                    LoadUnscheduledTripsAsync()
                );
            }
            catch (Exception ex)
            {
               
                MessageBox.Show($"An error occurred while loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                
                IsLoading = false;
            }
            
        }

        private async Task LoadRunsAndEventsAsync()
        {
            Runs.Clear();
            _masterAllEvents.Clear();
            foreach (var route in _allVehicleRoutes)
            {
                var runItem = new RunItemViewModel(route);
                try
                {
                    var schedules = await _scheduleService.GetSchedulesAsync(route.Id, CurrentDispatchDate);
                    foreach (var schedule in schedules)
                    {
                        // Enriquecemos el DTO con datos de la ruta para la vista "All Events"
                        /*schedule.Run = route.Name;
                        schedule.Driver = route.DriverFullName;
                        schedule.Vehicle = route.VehicleName;*/
                        _masterAllEvents.Add(schedule);
                    }
                    runItem.Schedules = new ObservableCollection<ScheduleDto>(schedules);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading schedules for route {route.Name}: {ex.Message}");
                }
                Runs.Add(runItem);
            }
            FilterAllEvents(); // Call the filter to populate the visible collection
        }

        private async Task LoadUnscheduledTripsAsync()
        {
            IsLoading = true;
            try
            {
                var unscheduledDtoList = await _scheduleService.GetUnscheduledTripsAsync(CurrentDispatchDate);

                var geocodingTasks = unscheduledDtoList.Select(trip => PopulateCitiesForTravel(trip)).ToList();
                await Task.WhenAll(geocodingTasks);

                _allUnscheduledTripsFromService.Clear();
                foreach (var dto in unscheduledDtoList)
                {
                    _allUnscheduledTripsFromService.Add(new TripItemViewModel(dto));
                }
                FilterUnscheduledTrips(); // Apply filter after loading
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error loading unscheduled trips: {ex.Message}");
            }
            finally
            {

                IsLoading = false;
            }
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

        /*private void FilterUnscheduledTrips()
        {
            var filteredTrips = _allUnscheduledTripsFromService.AsEnumerable();

            if (DisplayWillCalls)
            {
                filteredTrips = filteredTrips.Where(t => t.TripDto.WillCall);
            }

            // Actualizar la colección observable para la UI
            UnscheduledTrips.Clear();
            foreach (var trip in filteredTrips)
            {
                UnscheduledTrips.Add(trip);
            }
        }*/

        private void FilterUnscheduledTrips()
        {
            var filteredTrips = DisplayWillCalls
                ? _allUnscheduledTripsFromService.Where(t => t.TripDto.WillCall)
                : _allUnscheduledTripsFromService;

            UnscheduledTrips.Clear();
            foreach (var trip in filteredTrips)
            {
                UnscheduledTrips.Add(trip);
            }
        }

        private void FilterAllEvents()
        {
            IEnumerable<ScheduleDto> filtered = _masterAllEvents;

            if (!ShowPerformedEvents)
            {
                filtered = filtered.Where(e => !e.Performed);
            }
            // Aquí puedes añadir la lógica para los otros checkboxes cuando sea necesario
            // if (!ShowPullEvents) { ... }
            // if (ShowLatePickups) { ... }

            AllEvents.Clear();
            foreach (var item in filtered)
            {
                AllEvents.Add(item);
            }
            UpdateEventsSummary();
        }

        private void UpdateEventsSummary()
        {
            int total = _masterAllEvents.Count;
            int unperformed = _masterAllEvents.Count(e => !e.Performed);
            EventsSummaryText = $"{total} total events, {unperformed} unperformed";
        }

        #region Command Execution Methods
        private void ExecuteOverview(object parameter)
        {
            IsOverviewVisible = true;
        }

        private void ExecuteShowDispatchMain(object parameter)
        {
            IsOverviewVisible = false;
        }

        private async Task ExecuteExportScheduleAsync(object parameter)
        {
            Console.WriteLine("Export Schedule Clicked!");
            await Task.Delay(1);
        }

        private async Task ExecuteRefreshAsync(object parameter)
        {
            Console.WriteLine("Refresh Clicked!");
            await LoadAllDataAsync();
        }
        #endregion

        #region Translation 
        public string HeaderRuns => LocalizationService.Instance["Runs"]; 
        public string HeaderSelect => LocalizationService.Instance["Select"];
        public string HeaderUnscheduled => LocalizationService.Instance["Unscheduled"];
        public string HeaderDispatchMain => LocalizationService.Instance["DispatchMain"];

        public string SelectFieldsToDisplayToolTip => LocalizationService.Instance["SelectFieldsToDisplay"];

        public string ColumnHeaderOn => LocalizationService.Instance["On"];
        public string ColumnHeaderMTE => LocalizationService.Instance["MTE"];
        public string ColumnHeaderME => LocalizationService.Instance["ME"];
        public string ColumnHeaderTrips => LocalizationService.Instance["Trips"];
        public string ColumnHeaderUnperf => LocalizationService.Instance["Unperf"]; // Unperf.
        public string ColumnHeaderRun => LocalizationService.Instance["Run"];
        public string ColumnHeaderDriver => LocalizationService.Instance["Driver"];
        public string ColumnHeaderVehicle => LocalizationService.Instance["Vehicle"];
        public string ColumnHeaderLastNextEvent => LocalizationService.Instance["LastNextEvent"]; // "Last Event, Next Event"

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
        public string ColumnHeaderSpace => LocalizationService.Instance["SpaceType"];
        public string ColumnHeaderPickupComment => LocalizationService.Instance["PickupComment"];
        public string ColumnHeaderDropoffComment => LocalizationService.Instance["DropoffComment"];
        public string ColumnHeaderType => LocalizationService.Instance["Type"];
        public string ColumnHeaderDropoff => LocalizationService.Instance["Dropoff"];
        public string ColumnHeaderPickupPhone => LocalizationService.Instance["PickupPhone"];
        public string ColumnHeaderDropoffPhone => LocalizationService.Instance["DropoffPhone"];
        public string ColumnHeaderAuthorization => LocalizationService.Instance["Authorization"];
        public string ColumnHeaderFundingSource => LocalizationService.Instance["FundingSource"];
        public string ColumnHeaderDistance => LocalizationService.Instance["Distance"];
        public string ColumnHeaderPickupCity => LocalizationService.Instance["PickupCity"];
        public string ColumnHeaderDropoffCity => LocalizationService.Instance["DropoffCity"];

        // Actions     
        public string CancelTripToolTip => LocalizationService.Instance["CancelTrip"];
        public string EditTripToolTip => LocalizationService.Instance["Edit"];

        public string SuccessTitle => LocalizationService.Instance["SuccessTitle"]; // ej: "Éxito"
        public string ErrorTitle => LocalizationService.Instance["ErrorTitle"]; // ej: "Error"

        #endregion


    }
}