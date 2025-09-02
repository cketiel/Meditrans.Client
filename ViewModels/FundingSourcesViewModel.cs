using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Meditrans.Client.DTOs;
using Meditrans.Client.Models;
using Meditrans.Client.Services;
using Meditrans.Client.Views.Admin.Billing; // Reemplaza con la ruta correcta a tu popup
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Meditrans.Client.ViewModels
{
    public partial class FundingSourcesViewModel : ObservableObject
    {
        private readonly IFundingSourceService _fundingSourceService;
        private readonly IFundingSourceBillingItemService _fsBillingItemService;
        private readonly IBillingItemService _billingItemService;
        private readonly ISpaceTypeService _spaceTypeService;

        [ObservableProperty]
        private ObservableCollection<FundingSource> _fundingSources;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LoadFundingSourcesCommand))]
        private bool _displayInactive;

        [ObservableProperty]
        private ObservableCollection<FundingSourceBillingItem> _billingItemsForSelectedSource;

        [ObservableProperty]
        private bool _displayExpired;

        [ObservableProperty]
        private string _billingItemsTabHeader = "Billing Items";

        
        [ObservableProperty]
        private FundingSource _selectedFundingSource;

        public IAsyncRelayCommand LoadFundingSourcesCommand { get; }
        public IRelayCommand AddFundingSourceCommand { get; }
        public IRelayCommand<FundingSource> EditFundingSourceCommand { get; }
        public IAsyncRelayCommand ExportToExcelCommand { get; }

        public IAsyncRelayCommand LoadBillingItemsForSourceCommand { get; }
        public IRelayCommand AddBillingItemForSourceCommand { get; }
        public IRelayCommand<FundingSourceBillingItem> EditBillingItemForSourceCommand { get; }
        public IAsyncRelayCommand ExportBillingItemsForSourceCommand { get; }

        public FundingSourcesViewModel(
            IFundingSourceService fundingSourceService, 
            IFundingSourceBillingItemService fsBillingItemService,
            IBillingItemService billingItemService,
            ISpaceTypeService spaceTypeService)
        {
            _fundingSourceService = fundingSourceService;
            _fsBillingItemService = fsBillingItemService;
            _billingItemService = billingItemService;
            _spaceTypeService = spaceTypeService;

            _fundingSources = new ObservableCollection<FundingSource>();
            _billingItemsForSelectedSource = new ObservableCollection<FundingSourceBillingItem>();

            LoadFundingSourcesCommand = new AsyncRelayCommand(LoadFundingSourcesAsync);
            AddFundingSourceCommand = new RelayCommand(ExecuteAddFundingSource);
            EditFundingSourceCommand = new RelayCommand<FundingSource>(ExecuteEditFundingSource);
            ExportToExcelCommand = new AsyncRelayCommand(ExecuteExportToExcel);

            LoadBillingItemsForSourceCommand = new AsyncRelayCommand(LoadBillingItemsForSourceAsync);
            AddBillingItemForSourceCommand = new RelayCommand(ExecuteAddBillingItem, () => SelectedFundingSource != null);
            EditBillingItemForSourceCommand = new RelayCommand<FundingSourceBillingItem>(ExecuteEditBillingItem);
            ExportBillingItemsForSourceCommand = new AsyncRelayCommand(ExecuteExportBillingItems, () => BillingItemsForSelectedSource.Any());

        }

        //partial void OnDisplayInactiveChanged(bool value) => _ = LoadFundingSourcesAsync();

        // 4. Se ejecuta cuando el CheckBox "Display Expired" cambia
        partial void OnDisplayExpiredChanged(bool value) => _ = LoadBillingItemsForSourceAsync();

        // 5. Se ejecuta cuando el usuario selecciona una fila en el primer DataGrid
        async partial void OnSelectedFundingSourceChanged(FundingSource value)
        {
            // Actualiza el estado de los comandos que dependen de la selección
            AddBillingItemForSourceCommand.NotifyCanExecuteChanged();
            await LoadBillingItemsForSourceAsync();
        }

        // Se ejecuta cuando el valor de DisplayInactive cambia
        partial void OnDisplayInactiveChanged(bool value)
        {
            // Recarga los datos automáticamente
            _ = LoadFundingSourcesAsync();
        }

        private async Task LoadFundingSourcesAsync()
        {
            try
            {
                var items = await _fundingSourceService.GetFundingSourcesAsync(DisplayInactive);
                FundingSources.Clear();
                foreach (var item in items)
                {
                    FundingSources.Add(item);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"An error occurred while loading data: {ex.Message}", "Error");
            }
        }

        private async Task LoadBillingItemsForSourceAsync()
        {
            if (SelectedFundingSource == null)
            {
                BillingItemsForSelectedSource.Clear();
                BillingItemsTabHeader = "Billing Items";
                ExportBillingItemsForSourceCommand.NotifyCanExecuteChanged();
                return;
            }

            try
            {
                // 1. Recibimos la lista de DTOs
                var itemsDto = await _fsBillingItemService.GetByFundingSourceIdAsync(SelectedFundingSource.Id, DisplayExpired);

                BillingItemsForSelectedSource.Clear();

                // 2. Mapeamos cada DTO al modelo completo
                foreach (var dto in itemsDto)
                {
                    var model = new FundingSourceBillingItem
                    {
                        Id = dto.Id,
                        Rate = dto.Rate,
                        Per = dto.Per,
                        IsDefault = dto.IsDefault,
                        ProcedureCode = dto.ProcedureCode,
                        MinCharge = dto.MinCharge,
                        MaxCharge = dto.MaxCharge,
                        GreaterThanMinQty = dto.GreaterThanMinQty,
                        LessOrEqualMaxQty = dto.LessOrEqualMaxQty,
                        FreeQty = dto.FreeQty,
                        FromDate = dto.FromDate,
                        ToDate = dto.ToDate,

                        // Reconstruimos los objetos anidados para que los bindings del DataGrid funcionen
                        BillingItem = new BillingItem
                        {
                            Description = dto.BillingItemDescription,
                            Unit = new Unit { Abbreviation = dto.BillingItemUnitAbbreviation }
                        },
                        SpaceType = new SpaceType
                        {
                            Name = dto.SpaceTypeName
                        }
                    };
                    BillingItemsForSelectedSource.Add(model);
                }

                BillingItemsTabHeader = $"Billing Items ({BillingItemsForSelectedSource.Count})";
                ExportBillingItemsForSourceCommand.NotifyCanExecuteChanged();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading billing items: {ex.Message}", "Error");
            }
        }

        private void ExecuteAddBillingItem()
        {
            var newItem = new FundingSourceBillingItem
            {
                FundingSourceId = SelectedFundingSource.Id,
                FromDate = DateTime.Today,
                ToDate = DateTime.Today.AddYears(1)
            };
            OpenBillingItemPopup(newItem);
        }

        private void ExecuteEditBillingItem(FundingSourceBillingItem item)
        {
            if (item != null) OpenBillingItemPopup(item);
        }

        private void OpenBillingItemPopup(FundingSourceBillingItem item)
        {
            // Necesitaremos crear este ViewModel y la Vista
            var popupViewModel = new FundingSourceBillingItemPopupViewModel(_fsBillingItemService, _billingItemService, _spaceTypeService, item);
            var popupWindow = new FundingSourceBillingItemPopupWindow(popupViewModel);

            if (popupWindow.ShowDialog() == true)
            {
                _ = LoadBillingItemsForSourceAsync(); // Recargar al guardar
            }
        }

        private async Task ExecuteExportBillingItems()
        {
            await _fsBillingItemService.ExportToExcelAsync(BillingItemsForSelectedSource.ToList());
        }

        private void ExecuteAddFundingSource()
        {
            OpenPopup(new FundingSource { IsActive = true });
        }

        private void ExecuteEditFundingSource(FundingSource fundingSource)
        {
            if (fundingSource != null)
            {
                OpenPopup(fundingSource);
            }
        }

        private void OpenPopup(FundingSource fundingSource)
        {
            var popupViewModel = new FundingSourcePopupViewModel(_fundingSourceService, fundingSource);
            var popupWindow = new FundingSourcePopupWindow(popupViewModel); // Necesitarás crear esta vista

            if (popupWindow.ShowDialog() == true)
            {
                _ = LoadFundingSourcesAsync(); // Recargar la lista si se guardó
            }
        }

        private async Task ExecuteExportToExcel()
        {
            if (!FundingSources.Any())
            {
                MessageBox.Show("There is no data to export.", "Export");
                return;
            }

            try
            {
                await _fundingSourceService.ExportToExcelAsync(FundingSources.ToList());
                MessageBox.Show("Export successful!", "Export");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error");
            }
        }
    }
}