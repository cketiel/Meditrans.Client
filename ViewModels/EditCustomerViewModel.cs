// ViewModels/EditCustomerViewModel.cs

using Meditrans.Client.Commands; // Necesario para ICommand
using Meditrans.Client.Models;
using Meditrans.Client.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq; // Necesario para .FirstOrDefault()
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input; // Necesario para ICommand

namespace Meditrans.Client.ViewModels
{
    // Asumo que tu BaseViewModel implementa INotifyPropertyChanged
    public class EditCustomerViewModel : BaseViewModel
    {
        private Customer _customerToEdit;
        public Customer CustomerToEdit
        {
            get => _customerToEdit;
            set
            {
                _customerToEdit = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<FundingSource> FundingSources { get; set; }
        public ObservableCollection<SpaceType> SpaceTypes { get; set; }
        public ObservableCollection<string> Genders { get; set; }

        public EditCustomerViewModel(Customer originalCustomer)
        {
            // *** MEJORA: Usamos un clon para permitir la cancelación de cambios.
            CustomerToEdit = originalCustomer.Clone();

            FundingSources = new ObservableCollection<FundingSource>();
            SpaceTypes = new ObservableCollection<SpaceType>();
            Genders = new ObservableCollection<string> { "Male", "Female" };

            _ = LoadAuxDataAsync();
        }

        private async Task LoadAuxDataAsync()
        {
            // Cargar Funding Sources
            var fundingSourceService = new FundingSourceService();
            var sources = await fundingSourceService.GetFundingSourcesAsync();
            FundingSources.Clear();
            foreach (var source in sources)
            {
                FundingSources.Add(source);
            }
            // *** MEJORA: Seleccionar el FundingSource actual del cliente en el ComboBox ***
            if (CustomerToEdit.FundingSourceId > 0)
            {
                CustomerToEdit.FundingSource = FundingSources.FirstOrDefault(fs => fs.Id == CustomerToEdit.FundingSourceId);
            }

            // Cargar Space Types
            var spaceTypeService = new SpaceTypeService(); // Asegúrate de que este servicio existe
            var types = await spaceTypeService.GetSpaceTypesAsync(); // Y este método también
            SpaceTypes.Clear();
            foreach (var type in types)
            {
                SpaceTypes.Add(type);
            }
            // *** MEJORA: Seleccionar el SpaceType actual del cliente en el ComboBox ***
            if (CustomerToEdit.SpaceTypeId > 0)
            {
                CustomerToEdit.SpaceType = SpaceTypes.FirstOrDefault(st => st.Id == CustomerToEdit.SpaceTypeId);
            }

            // Notificar a la UI que las propiedades de CustomerToEdit pueden haber cambiado.
            OnPropertyChanged(nameof(CustomerToEdit));
        }

        public void ApplyChanges(Customer originalCustomer)
        {
            // *** MEJORA: Mapear los cambios del clon al objeto original antes de guardar.
            // Esto asegura que los Ids correctos se guardan en la BD.
            if (CustomerToEdit.FundingSource != null)
            {
                CustomerToEdit.FundingSourceId = CustomerToEdit.FundingSource.Id;
            }
            if (CustomerToEdit.SpaceType != null)
            {
                CustomerToEdit.SpaceTypeId = CustomerToEdit.SpaceType.Id;
            }

            // Copiar todas las propiedades del clon al original
            originalCustomer.FullName = CustomerToEdit.FullName;
            originalCustomer.Address = CustomerToEdit.Address;
            originalCustomer.City = CustomerToEdit.City;
            originalCustomer.State = CustomerToEdit.State;
            originalCustomer.Zip = CustomerToEdit.Zip;
            originalCustomer.Phone = CustomerToEdit.Phone;
            originalCustomer.MobilePhone = CustomerToEdit.MobilePhone;
            originalCustomer.ClientCode = CustomerToEdit.ClientCode;
            originalCustomer.PolicyNumber = CustomerToEdit.PolicyNumber;
            originalCustomer.Email = CustomerToEdit.Email;
            originalCustomer.DOB = CustomerToEdit.DOB;
            originalCustomer.Gender = CustomerToEdit.Gender;
            originalCustomer.FundingSourceId = CustomerToEdit.FundingSourceId;
            originalCustomer.SpaceTypeId = CustomerToEdit.SpaceTypeId;
            // No se copian Created, CreatedBy, etc., ya que no deberían ser editables.
        }
    }
}