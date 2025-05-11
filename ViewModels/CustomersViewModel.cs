using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Windows.Input;
using Meditrans.Client.Models;
using Meditrans.Client.Services;
using System.Threading.Tasks;
using Meditrans.Client.Commands;
using Meditrans.Client.Helpers;
using System.Windows;

namespace Meditrans.Client.ViewModels
{
    public class CustomersViewModel : BaseViewModel
    {
        #region Translation

        // Filters
        public string FullNameText => LocalizationService.Instance["FullName"];
        public string PhoneText => LocalizationService.Instance["Phone"];
        public string ClientCode => LocalizationService.Instance["ClientCode"];
        public string SelectFundingSource => LocalizationService.Instance["SelectFundingSource"];
        public string Search => LocalizationService.Instance["Search"];

        // Actions
        public string ExcelExport => LocalizationService.Instance["ExcelExport"];
        public string Edit => LocalizationService.Instance["Edit"];

        // DataGrid Column Header
        public string AddressText => LocalizationService.Instance["Address"];
        public string CityText => LocalizationService.Instance["City"];
        public string StateText => LocalizationService.Instance["State"];
        public string ZipText => LocalizationService.Instance["Zip"];
        public string MobilePhoneText => LocalizationService.Instance["MobilePhone"];
        public string ClientCodeText => LocalizationService.Instance["ClientCode"];
        public string PolicyNumberText => LocalizationService.Instance["PolicyNumber"];
        public string FundingSourceText => LocalizationService.Instance["FundingSource"];
        public string SpaceTypeText => LocalizationService.Instance["SpaceType"];
        public string EmailText => LocalizationService.Instance["Email"];
        public string DOBText => LocalizationService.Instance["DOB"];
        public string GenderText => LocalizationService.Instance["Gender"];
        public string CreatedText => LocalizationService.Instance["Created"];
        public string CreatedByText => LocalizationService.Instance["CreatedBy"];


        #endregion

        private readonly ICustomerService _customerService;
    
        private ObservableCollection<Customer> _customers;
        private ObservableCollection<Customer> _filteredCustomers;
        public ObservableCollection<FundingSource> FundingSources { get; set; } = new();

        public ObservableCollection<Customer> Customers
        {
            get => _filteredCustomers;
            set { _filteredCustomers = value; OnPropertyChanged(); }
        }

        public string FilterFullName { get; set; }
        public string FilterPhone { get; set; }
        public string FilterClientCode { get; set; }

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


        public ICommand SearchCommand { get; }
        public ICommand EditCustomerCommand { get; }
        public ICommand ExportToExcelCommand { get; }

        public CustomersViewModel()
        {            
            _customers = new ObservableCollection<Customer>();
            _filteredCustomers = new ObservableCollection<Customer>();
            FundingSources = new ObservableCollection<FundingSource>();

            SearchCommand = new RelayCommandObject(async _ => await ApplyFilters());
            EditCustomerCommand = new RelayCommandObject(EditCustomer, _ => SelectedCustomer != null);
            ExportToExcelCommand = new RelayCommandObject(_ => ExportToExcel());
            _ = LoadDataAsync();
        }

        private Customer _selectedCustomer;
        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set { _selectedCustomer = value; OnPropertyChanged(); }
        }

        private async Task LoadDataAsync()
        {
            /*CustomerService _customerService = new CustomerService();
            var list = await _customerService.LoadAllCustomersAsync();
            _customers = new ObservableCollection<Customer>(list);
            Customers = new ObservableCollection<Customer>(_customers);
            OnPropertyChanged(nameof(Customers));*/

            FundingSourceService _fundingSourceService = new FundingSourceService();
            var sources = await _fundingSourceService.GetFundingSourcesAsync();
            FundingSources.Clear();
            foreach (var source in sources)
                FundingSources.Add(source);
        }

        private Task ApplyFilters()
        {
            if(_customers.Count > 0)
            {
                var query = _customers.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(FilterFullName))
                    query = query.Where(c => c.FullName?.Contains(FilterFullName, System.StringComparison.OrdinalIgnoreCase) == true);
                if (!string.IsNullOrWhiteSpace(FilterPhone))
                    query = query.Where(c => c.Phone?.Contains(FilterPhone) == true);
                if (!string.IsNullOrWhiteSpace(FilterClientCode))
                    query = query.Where(c => c.ClientCode?.Contains(FilterClientCode, System.StringComparison.OrdinalIgnoreCase) == true);
                if (SelectedFundingSource != null)
                    query = query.Where(c => c.FundingSource?.Id == SelectedFundingSource.Id);

                Customers = new ObservableCollection<Customer>(query);
                OnPropertyChanged(nameof(Customers));
            }
            else
            {
                LoadAllCustomers();
            }

            return Task.CompletedTask;
        }

        private async Task LoadAllCustomers()
        {
            CustomerService _customerService = new CustomerService();
            var list = await _customerService.GetAllCustomersAsync();
            //var list = await _customerService.LoadAllCustomersAsync();
            _customers = new ObservableCollection<Customer>(list);
            Customers = new ObservableCollection<Customer>(_customers);
            OnPropertyChanged(nameof(Customers));
        }

        private void EditCustomer(object obj)
        {
            MessageBox.Show("EditCustomer");
            // Aquí puedes navegar o abrir un modal con los datos de SelectedCustomer
        }

        private void ExportToExcel()
        {
            MessageBox.Show("ExportToExcel");
            // TODO: implementar exportación real
            // por ahora podrías levantar un diálogo o guardar un archivo
        }
    }
}
