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
using Meditrans.Client.Helpers;
using Meditrans.Client.Models;
using Meditrans.Client.Services;

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
        public string ChargesPerText => LocalizationService.Instance["ChargesPerText"]; // Per
        public string ChargesCostText => LocalizationService.Instance["ChargesCostText"]; // Cost


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

        private FundingSourceBillingItem _charges;
        public FundingSourceBillingItem SelectedCharges // Hay que armar Qty y Cost segun el tipo de BillingItem. Y Se debe actualizar cuando se editen: Distance, Pickup Address, Dropoof Address, (ya que en estos 2 ultimos se recalcula la distacia)
        {
            get => _charges;
            set
            {
                _charges = value;
                OnPropertyChanged();
            }
        }

        #endregion

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

        private DateTime _filterDate;
        public DateTime FilterDate
        {
            get => _filterDate;
            set
            {
                _filterDate = value;
                OnPropertyChanged();
            }
            /*set
            {
                _filterDate = value;
                OnPropertyChanged(nameof(FilterDate));
            }*/
        }


        /*protected virtual bool SetProperty<T>(ref T member, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(member, value))
            {
                return false;
            }

            member = value;
            OnPropertyChanged(propertyName);
            return true;
        }*/

        private string? _name;
        public string? Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        //public event PropertyChangedEventHandler PropertyChanged;

        //public ObservableCollection<Trip> Trips { get; set; }
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
            /*set
            {
                _selectedCustomer = value; MessageBox.Show("entro en Set Selected Customer " + value?.FullName);
                if (SetProperty(ref _selectedCustomer, value))
                {
                    // Lógica adicional al seleccionar
                }
            }*/
            set
            {
                _selectedCustomer = value; 
                OnPropertyChanged();              
                if (value != null)
                {
                    SearchText = value.FullName; 
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
            /*set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterCustomers();
                }
            }*/
            set
            {
                _searchText = value;
                OnPropertyChanged();
                if (string.IsNullOrEmpty(value))
                {
                    SelectedCustomer = null; //MessageBox.Show("SearchText null");
                    flag = true;
                } 
                //if(flag)
                    FilterCustomers();
                //flag = false;
            }
        }

        public bool flag = true;




        // --------------





        private string _customerSearchText;
        public string CustomerSearchText
        {
            get => _customerSearchText;
            set
            {
                _customerSearchText = value;
                OnPropertyChanged();
                UpdateFilteredCustomers();
            }
        }

        //public ObservableCollection<Customer> Customers { get; set; } = new();

        //public ObservableCollection<Customer> FilteredCustomers { get; set; } = new();

        // Campos individuales para edición
        /*public string FullName { get; set; }
        public string ClientCode { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Phone { get; set; }
        public string MobilePhone { get; set; }
        public DateTime? DOB { get; set; } = DateTime.Today;
        public string Gender { get; set; } = "Male";*/

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
            }
        }

        #endregion

        public DateTime? TripFilterDate { get; set; } = DateTime.Today;
        public bool ShowCanceled { get; set; }

        // === Commands ===
        public ICommand SaveCustomerCommand { get; }
        public ICommand NewCustomerCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }


        public HomeViewModel() {

            FilterDate = DateTime.Today; // DateTime.Now

            //SaveCustomerCommand = new RelayCommand(SaveCustomer);
            NewCustomerCommand = new RelayCommand(NewCustomer);
            ImportCommand = new RelayCommand(ImportTrips);
            ExportCommand = new RelayCommand(ExportTrips);

            LoadData();
            InitializeData();

            // Suscripción manual para debug
            /*this.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(SelectedCustomer))
                    MessageBox.Show($"Customer seleccionado: {SelectedCustomer?.FullName}");
                    //Debug.WriteLine($"Customer seleccionado: {SelectedCustomer?.FullName}");
            };*/


            //GetTrips();
            /*var trips = new ObservableCollection<Trip>
            {
                new Trip { SpaceType = "AMB", PatientName="Alicia Ambrose", Date="2025-04-04", FromTime="09:10 AM", PickupAddress="1401 16th Street, Sarasota, FL 34236, EE. UU.", DropoffAddress="240b South Tuttle Avenue, Sarasota, FL 34237, EE. UU.", PickupLatitude=27.351814, PickupLongitude=-82.542549, DropoffLatitude=27.334042, DropoffLongitude=-82.514795 },
                new Trip { SpaceType = "AMB", PatientName="Alicia Ambrose", Date="2025-04-04", FromTime="01:00 PM", PickupAddress="240b South Tuttle Avenue, Sarasota, FL 34237, EE. UU.", DropoffAddress="1401 16th Street, Sarasota, FL 34236, EE. UU.", PickupLatitude=27.334042, PickupLongitude=-82.514795, DropoffLatitude=27.351814, DropoffLongitude=-82.542549 },

                new Trip { SpaceType = "AMB", PatientName="Regina Baker", Date="2025-04-04", FromTime="08:50 AM", PickupAddress="7059 Jarvis Road, Sarasota, FL 34241, EE. UU.", DropoffAddress="2540 South Tamiami Trail, Sarasota, FL 34239, EE. UU.", PickupLatitude=27.292011, PickupLongitude=-82.429845, DropoffLatitude=27.310791, DropoffLongitude=-82.530251 },
                new Trip { SpaceType = "AMB", PatientName="Regina Baker", Date="2025-04-04", FromTime="11:31 AM", PickupAddress="2540 South Tamiami Trail, Sarasota, FL 34239, EE. UU.", DropoffAddress="7059 Jarvis Road, Sarasota, FL 34241, EE. UU.", PickupLatitude=27.310791, PickupLongitude=-82.530251, DropoffLatitude=27.292011, DropoffLongitude=-82.429845 }

            };
            Trips = trips;
            SelectedTrip = Trips[0];*/
        }

        private async void InitializeData()
        {
            TripType = Meditrans.Client.Models.TripType.Appointment;
        }
        private async void LoadData()
        {
            await LoadFundingSourcesAsync();
            await LoadSpaceTypesAsync();

            await LoadCustomersFromApi();

            //FilteredCustomers = Customers;
            //UpdateFilteredCustomers();

            // Simular algunos viajes
            //Trips.Add(new Trip { PatientName = "John Smith", Date = DateTime.Today, FromTime = "10:00 AM", PickupAddress = "123 Main St", DropoffAddress = "456 Clinic Rd" });
            //Trips.Add(new Trip { PatientName = "Maria Lopez", Date = DateTime.Today, FromTime = "11:30 AM", PickupAddress = "78 Elm St", DropoffAddress = "Hospital Ave" });
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
            var sources = await customerService.GetAllAsync();
            foreach (var source in sources)
                _customers.Add(source);

            Customers = new ObservableCollection<Customer>(_customers);
            FilteredCustomers = new ObservableCollection<Customer>(_customers);
            
            /*Customers.Clear();
            foreach (var source in sources)
                Customers.Add(source);

            SelectedCustomer = Customers.First();*/


        }

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

        private void UpdateFilteredCustomers()
        {
            var filtered = string.IsNullOrWhiteSpace(CustomerSearchText)
                ? Customers
                : new ObservableCollection<Customer>(
                    Customers.Where(c =>
                        c.FullName.Contains(CustomerSearchText, StringComparison.InvariantCultureIgnoreCase) ||
                        c.ClientCode.Contains(CustomerSearchText, StringComparison.InvariantCultureIgnoreCase) /*||
                        c.MobilePhone.Contains(CustomerSearchText, StringComparison.InvariantCultureIgnoreCase)*/ ));

            FilteredCustomers.Clear();
            foreach (var c in filtered)
                FilteredCustomers.Add(c);
        }

        /*private void FillCustomerFields(Customer customer)
        {
            FullName = customer.FullName;
            ClientCode = customer.ClientCode;
            Address = customer.Address;
            City = customer.City;
            State = customer.State;
            Zip = customer.Zip;
            Phone = customer.Phone;
            MobilePhone = customer.MobilePhone;
            DOB = customer.DOB;
            Gender = customer.Gender;
            SelectedSpaceType = customer.SpaceType;
            SelectedFundingSource = customer.FundingSource;

            OnPropertyChanged(nameof(FullName));
            OnPropertyChanged(nameof(ClientCode));
            OnPropertyChanged(nameof(Address));
            OnPropertyChanged(nameof(City));
            OnPropertyChanged(nameof(State));
            OnPropertyChanged(nameof(Zip));
            OnPropertyChanged(nameof(Phone));
            OnPropertyChanged(nameof(MobilePhone));
            OnPropertyChanged(nameof(DOB));
            OnPropertyChanged(nameof(Gender));
            OnPropertyChanged(nameof(SelectedSpaceType));
            OnPropertyChanged(nameof(SelectedFundingSource));
        }*/

        private void SaveCustomer()
        {
            /*if (SelectedCustomer == null)
            {
                var newCustomer = new Customer();
                Customers.Add(newCustomer);
                SelectedCustomer = newCustomer;
            }

            SelectedCustomer.FullName = FullName;
            SelectedCustomer.ClientCode = ClientCode;
            SelectedCustomer.Address = Address;
            SelectedCustomer.City = City;
            SelectedCustomer.State = State;
            SelectedCustomer.Zip = Zip;
            SelectedCustomer.Phone = Phone;
            SelectedCustomer.MobilePhone = MobilePhone;
            SelectedCustomer.DOB = DOB;
            SelectedCustomer.Gender = Gender;
            //SelectedCustomer.SpaceType = SelectedSpaceType;
            //SelectedCustomer.FundingSource = SelectedFundingSource;

            UpdateFilteredCustomers();*/
        }

        private void NewCustomer()
        {
            /*SelectedCustomer = null;
            FullName = "";
            ClientCode = "";
            Address = "";
            City = "";
            State = "";
            Zip = "";
            Phone = "";
            MobilePhone = "";
            DOB = DateTime.Today;
            Gender = "Male";
            SelectedSpaceType = null;
            SelectedFundingSource = null;

            OnPropertyChanged(nameof(FullName));
            OnPropertyChanged(nameof(ClientCode));
            OnPropertyChanged(nameof(Address));
            OnPropertyChanged(nameof(City));
            OnPropertyChanged(nameof(State));
            OnPropertyChanged(nameof(Zip));
            OnPropertyChanged(nameof(Phone));
            OnPropertyChanged(nameof(MobilePhone));
            OnPropertyChanged(nameof(DOB));
            OnPropertyChanged(nameof(Gender));
            OnPropertyChanged(nameof(SelectedSpaceType));
            OnPropertyChanged(nameof(SelectedFundingSource));*/
        }

        private void ImportTrips()
        {
            // Por implementar
        }

        private void ExportTrips()
        {
            // Por implementar
        }


        public async void GetTrips()
        {
            var tripService = new TripService();
            Trips = await tripService.GetTripsAsync(); 
            SelectedTrip = Trips[0];
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

        
        /*protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }*/

    }
}
