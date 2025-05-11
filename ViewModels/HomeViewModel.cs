using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using Meditrans.Client.Exceptions;
using Meditrans.Client.Helpers;
using Meditrans.Client.Models;
using Meditrans.Client.Services;
using Newtonsoft.Json.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Meditrans.Client.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        #region Translation

        // Billing section
        public string Charges => LocalizationService.Instance["Charges"];
        public string History => LocalizationService.Instance["History"];
        public string Signature => LocalizationService.Instance["Signature"];
        public string TotalTripChargeTextLabel => LocalizationService.Instance["TotalTripChargeTextLabel"]; // Total Trip Charge
        public string DistanceTextLabel => LocalizationService.Instance["DistanceTextLabel"]; // Distance
        public string Imported => LocalizationService.Instance["Imported"];
        public string Routed => LocalizationService.Instance["Routed"];
        public string SelectedChargesTextLabel => LocalizationService.Instance["SelectedChargesTextLabel"]; // Selected Charges
        public string AllowUpdateText => LocalizationService.Instance["AllowUpdateText"]; // Allow Update

        public string ChargesDescriptionText => LocalizationService.Instance["ChargesDescriptionText"]; // Description
        public string ChargesRateText => LocalizationService.Instance["ChargesRateText"]; // Rate
        public string ChargesQtyText => LocalizationService.Instance["ChargesQtyText"]; // Qty
        public string ChargesPerText => LocalizationService.Instance["ChargesPerText"]; // Unit
        public string ChargesProcedureCodeText => LocalizationService.Instance["ChargesProcedureCodeText"]; // Code
        public string ChargesCostText => LocalizationService.Instance["ChargesCostText"]; // Cost
        public string NonDefaultChargesTextLabel => LocalizationService.Instance["NonDefaultChargesTextLabel"]; // Available, Non-default Charges
        public string IsDefaultText => LocalizationService.Instance["IsDefaultText"]; // Is Default
        public string FundingSourceText => LocalizationService.Instance["FundingSource"];
        public string AuthorizationText => LocalizationService.Instance["Authorization"];

        #endregion

        #region Billing Section

        private string _textHeaderBillingSection;
        public string TextHeaderBillingSection
        {
            get => _textHeaderBillingSection;
            set
            {
                _textHeaderBillingSection = value;
                OnPropertyChanged();
            }
        }
       
        private double _totalTripCharge;  
        public double TotalTripCharge
        {
            //get => "$" + _textTotalTripCharge.ToString();
            get => _totalTripCharge;
            set
            {
                _totalTripCharge = value;
                OnPropertyChanged();
            }

        }
        
        private string _tripType;
        public string TripType
        {
            get => _tripType; // LocalizationService.Instance[_tripType]; // _tripType;
            set
            {
                _tripType = value;
                OnPropertyChanged();
            }
        }

        private string _distance;
        public string Distance
        {
            get => _distance;
            set
            {
                _distance = value;
                OnPropertyChanged();
                UpdateSelectedCharges();
            }
        }
        
        private string _eta;
        public string ETA
        {
            get => _eta;
            set
            {
                _eta = value;
                OnPropertyChanged();
            }
        }

        private bool _originImported;
        public bool OriginImported
        {
            get => _originImported;
            set
            {
                _originImported = value;
                OnPropertyChanged();
            }
        }
        
        private bool _originRouted;
        public bool OriginRouted
        {
            get => _originRouted;
            set
            {
                _originRouted = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<FundingSourceBillingItem> _allFundingSourceBillingItem;

        //private ObservableCollection<FundingSourceBillingItem> _charges;
        public ObservableCollection<FundingSourceBillingItem> SelectedCharges { get; set; } = new(); // Hay que armar Qty y Cost segun el tipo de BillingItem. Y Se debe actualizar cuando se editen: Distance, Pickup Address, Dropoof Address, (ya que en estos 2 ultimos se recalcula la distacia)


        //private ObservableCollection<FundingSourceBillingItem> _nonDefaultCharges;
        public ObservableCollection<FundingSourceBillingItem> NonDefaultCharges { get; set; } = new();


        private string _authorization;
        public string Authorization
        {
            get => _authorization;
            set
            {
                _authorization = value;
                OnPropertyChanged();
            }
        }

        public void UpdateSelectedCharges()
        {
            if (_allFundingSourceBillingItem != null) {
                var selected = (SelectedFundingSource == null && SelectedSpaceType == null) ? _allFundingSourceBillingItem : new ObservableCollection<FundingSourceBillingItem>(
                    _allFundingSourceBillingItem.Where(f => f.FundingSourceId == SelectedFundingSource?.Id && f.SpaceTypeId == SelectedSpaceType?.Id && f.IsDefault == true));

                if (selected != null)
                {
                    SelectedCharges.Clear();
                    foreach (var c in selected)
                    {
                        if (c.BillingItem.Unit.Abbreviation == "MILE")
                        {
                            string[] parts = Distance.Split(' '); // format => 11.5 mi
                            c.Qty = decimal.Parse(parts[0]);
                        }
                        else if (c.BillingItem.Unit.Abbreviation == "UNIT")
                        {
                            c.Qty = 1;
                        }
                        else
                        {
                            c.Qty = 1; // Despues ver todas las opciones, por el momento, para que no de error de referencia.
                        }

                        c.Cost = c.Rate * c.Qty;
                        SelectedCharges.Add(c);

                    }
                }
            }
                     
            
        }

        public void UpdateNonDefaultCharges()
        {
            if (_allFundingSourceBillingItem != null) {
                var nonDefault = (SelectedFundingSource == null && SelectedSpaceType == null) ? _allFundingSourceBillingItem : new ObservableCollection<FundingSourceBillingItem>(
                _allFundingSourceBillingItem.Where(f => f.FundingSourceId == SelectedFundingSource?.Id && f.SpaceTypeId == SelectedSpaceType?.Id && f.IsDefault != true));

                if (nonDefault != null)
                {
                    NonDefaultCharges.Clear();
                    foreach (var s in nonDefault)
                    {
                        NonDefaultCharges.Add(s);
                    }
                }
            }
            
            
        }

        #endregion

        #region Trip

        public TimeSpan FromTime { get; set; }

        private ObservableCollection<Trip> _trips;
        public ObservableCollection<Trip> Trips
        {
            get => _trips;
            set
            {
                _trips = value;
                OnPropertyChanged(nameof(Trips)); // Notifica al DataGrid que la lista ha cambiado
            }
        }

        private Trip _selectedTrip;
        public Trip SelectedTrip
        {
            get => _selectedTrip;
            set
            {
                _selectedTrip = value;
                OnPropertyChanged();
            }
        }

        private DateTime _filterDate;
        public DateTime FilterDate
        {
            get => _filterDate;
            set
            {
                _filterDate = value;
                OnPropertyChanged();
            }
        }

        public DateTime? TripFilterDate { get; set; } = DateTime.Today;
        public bool ShowCanceled { get; set; }

        private string _drogoffAddress;
        public string DrogoffAddress
        {
            get => _drogoffAddress;
            set
            {
                _drogoffAddress = value;
                OnPropertyChanged();
                //UpdateSelectedCharges();
            }
        }

        // Trip data section

        private TimeSpan _pickupTimePicker;

        public TimeSpan PickupTimePicker
        {
            get => _pickupTimePicker;
            set
            {
                _pickupTimePicker = value;
                OnPropertyChanged();
            }
        }

        private TimeSpan _apptTimePicker;

        public TimeSpan ApptTimePicker
        {
            get => _apptTimePicker;
            set
            {
                _apptTimePicker = value;
                OnPropertyChanged();
            }
        }

        private TimeSpan _returnTimePicker;

        public TimeSpan ReturnTimePicker
        {
            get => _returnTimePicker;
            set
            {
                _returnTimePicker = value;
                OnPropertyChanged();
            }
        }

        /*private string? _name;
        public string? Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }*/

        #endregion

        #region Customer

        private ObservableCollection<Customer> _customers;
        private ObservableCollection<Customer> _filteredCustomers;
        private Customer _selectedCustomer;
        private string _searchText;

        public ObservableCollection<Customer> Customers
        {
            get => _customers;           
            set
            {
                _customers = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Customer> FilteredCustomers
        {
            get => _filteredCustomers;
            //set => SetProperty(ref _filteredCustomers, value);
            set
            {
                _filteredCustomers = value;
                OnPropertyChanged();
            }
        }

        public Customer SelectedCustomer
        {
            get => _selectedCustomer;           
            set
            {
                _selectedCustomer = value; 
                OnPropertyChanged();              
                if (value != null)
                {
                    SearchText = value.FullName; // Patch to autocomplete bug.
                }
                if(_selectedCustomer != null)
                {
                    SelectedSpaceType = SpaceTypes.FirstOrDefault(s => s.Id == _selectedCustomer.SpaceTypeId);
                    SelectedFundingSource = FundingSources.FirstOrDefault(f => f.Id == _selectedCustomer.FundingSourceId);
                    // That doesn't work
                    //SelectedFundingSource = (FundingSource)_selectedCustomer.FundingSource;
                    GenderMale = _selectedCustomer.Gender == Gender.Male;
                    GenderFemale = _selectedCustomer.Gender == Gender.Female;
                }
                else
                {
                    SelectedSpaceType = null;
                    SelectedFundingSource = null;
                }
            }
        }
       // public bool IsCustomerSelected => SelectedCustomer != null;

        /*public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                _selectedCustomer = value;
                OnPropertyChanged();
                if (value != null)
                {
                    FillCustomerFields(value);
                }
            }
        }*/

        public string SearchText
        {
            get => _searchText;            
            set
            {
                _searchText = value;
                OnPropertyChanged();
                if (string.IsNullOrEmpty(value))
                {
                    SelectedCustomer = null; 
                } 
                FilterCustomers();              
            }
        }

        // Space Types
        public ObservableCollection<SpaceType> SpaceTypes { get; set; } = new();

        private SpaceType _selectedSpaceType;
        public SpaceType SelectedSpaceType
        {
            get => _selectedSpaceType;
            set
            {
                _selectedSpaceType = value;
                OnPropertyChanged();
                UpdateSelectedCharges();
                UpdateNonDefaultCharges();
            }
        }

        // Funding Sources
        public ObservableCollection<FundingSource> FundingSources { get; set; } = new();

        private FundingSource _selectedFundingSource;
        public FundingSource SelectedFundingSource
        {
            get => _selectedFundingSource;
            set
            {
                _selectedFundingSource = value;
                OnPropertyChanged();
                UpdateSelectedCharges();
                UpdateNonDefaultCharges();
            }
        }

        private bool _genderMale;
        public bool GenderMale
        {
            get => _genderMale;
            set
            {
                _genderMale = value;
                OnPropertyChanged();
            }
        }
        private bool _genderFemale;
        public bool GenderFemale
        {
            get => _genderFemale;
            set
            {
                _genderFemale = value;
                OnPropertyChanged();
            }
        }

        private int _idCustomer;
        public int IdCustomer
        {
            get => _idCustomer;
            set
            {
                _idCustomer = value;
                OnPropertyChanged();
            }
        }

        #endregion

        // === Commands ===
        public ICommand SaveCustomerCommand { get; }
        public ICommand NewCustomerCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }

        public ICommand SaveTripCommand { get; }

        public HomeViewModel() {

            FilterDate = DateTime.Today; // DateTime.Now
            
            //SaveCustomerCommand = new RelayCommand(SaveCustomer);
            NewCustomerCommand = new RelayCommand(NewCustomer);
            ImportCommand = new RelayCommand(ImportTrips);
            ExportCommand = new RelayCommand(ExportTrips);
            SaveTripCommand = new RelayCommand(SaveTrip);

            LoadData();
            InitializeData();

            // Manual subscription for debug
            /*this.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(SelectedCustomer))
                    MessageBox.Show($"Customer seleccionado: {SelectedCustomer?.FullName}");
                    //Debug.WriteLine($"Customer seleccionado: {SelectedCustomer?.FullName}");
            };*/

        }

        #region API
        public async Task LoadFundingSourceBillingItemAsync()
        {
            IFundingSourceBillingItemService _fundingSourceService = new FundingSourceBillingItemService();
            var sources = await _fundingSourceService.GetAllAsync();
            
            foreach (var source in sources)
                _allFundingSourceBillingItem.Add(source);
        }
        public async Task LoadSpaceTypesAsync()
        {
            SpaceTypeService _spaceTypeService = new SpaceTypeService();
            var sources = await _spaceTypeService.GetSpaceTypesAsync();
            SpaceTypes.Clear();
            foreach (var source in sources)
            {
                source.ShowNameDescription = source.Description + " " + source.Name;
                SpaceTypes.Add(source);
            }
                
        }
        public async Task LoadFundingSourcesAsync()
        {
            FundingSourceService _fundingSourceService = new FundingSourceService();
            var sources = await _fundingSourceService.GetFundingSourcesAsync();
            FundingSources.Clear();
            foreach (var source in sources)
                FundingSources.Add(source);
        }
        public async Task LoadCustomersFromApi()
        {
            _customers = new ObservableCollection<Customer>();           
            var customerService = new CustomerService();           
            try
            {
                var sources = await customerService.GetAllCustomersAsync();
                //var sources = await customerService.GetAllAsync();
                foreach (var source in sources)
                    _customers.Add(source);

                Customers = new ObservableCollection<Customer>(_customers);
                FilteredCustomers = new ObservableCollection<Customer>(_customers);
                
            }
            catch (ApiException ex)
            {
                MessageBox.Show(
                    $"Error {ex.StatusCode}:\n{ex.ErrorDetails}",
                    "Error del servidor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error inesperado: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

        }
        public async void LoadTripsFromApi()
        {
            var tripList = new List<Trip>();
            var tripService = new TripService();
            tripList = await tripService.GetTripList();
            if (tripList != null && tripList.Any())
            {
                Trips = new ObservableCollection<Trip>(tripList);
                SelectedTrip = Trips.First(); // 
            }
            else
            {
                // Show message or log: no data arrived
            }
        }

        #endregion

        #region Filters
        private void FilterCustomers()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredCustomers = new ObservableCollection<Customer>(_customers);
                return;
            }

            var searchLower = SearchText.Trim().ToLower();  // Optimization: pre-process the text. Avoid multiple calls to Trim() and ToLower()
            //Other type of filter (c.FullName.StartsWith)
            var filtered = _customers
                .Where(c => (c.FullName?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false)
                         || (c.ClientCode?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false)
                         || (c.Phone?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            FilteredCustomers = new ObservableCollection<Customer>(filtered);
            //MessageBox.Show(FilteredCustomers.Count().ToString() + " cantidad de customer");
        }

        #endregion

        #region Class Methods
        private async void InitializeData()
        {
            TripType = Meditrans.Client.Models.TripType.Appointment;
        }
        private void LoadData()
        {
            LoadFundingSourcesAsync();
            LoadSpaceTypesAsync();
            LoadCustomersFromApi();
            LoadFundingSourceBillingItemAsync();
        }

        private void SaveCustomer()
        {
            
        }

        private void NewCustomer()
        {
            
        }

        private void ImportTrips()
        {
            // Por implementar
        }

        private void ExportTrips()
        {
            // Por implementar
        }

        private async void SaveTrip()
        {
            MessageBox.Show("save trip");
            Trip trip = new Trip();
            trip.Date = FilterDate;
            trip.Day = FilterDate.Date.DayOfWeek.ToString();
            trip.FromTime = PickupTimePicker;
            trip.ToTime = ApptTimePicker;
            trip.CustomerId = IdCustomer;

            //trip.PickupAddress = pickupAd           
            /*trip.PickupLatitude
            trip.PickupLongitude
            trip.Pickup
            trip.PickupPhone
            trip.PickupComment
                
            trip.DropoffAddress = DrogoffAddress;
            trip.DropoffLatitude
            trip.DropoffLongitude
            trip.Dropoof
            trip.DropoofPhone
            trip.DropoofComment

            trip.SpaceTypeId = SelectedSpaceType.Id;

            trip.Charge
            trip.Paid
            trip.Type
            trip.TripId
            trip.Authorization
            trip.Distance
            trip.ETA
            trip.WillCall
            trip.Status
            trip.Created
            trip.FundingSourceId*/


            TripService _tripService = new TripService();

            try
            {
                var createdCustomer = await _tripService.CreateTripAsync(trip);
                //success = true;
                //vm.IdCustomer = createdCustomer.Id;
            }
            catch (ApiException ex)
            {
                MessageBox.Show(
                    $"Error {ex.StatusCode}:\n{ex.ErrorDetails}",
                    "Error del servidor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error inesperado: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

    }
}
