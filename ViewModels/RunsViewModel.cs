using Meditrans.Client.Models;
using Meditrans.Client.Services;
using Meditrans.Client.Views.Data.Scheduling;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.Windows.Input;
using Meditrans.Client.Commands;
using Meditrans.Client.Exceptions;

namespace Meditrans.Client.ViewModels
{
    public class RunsViewModel : BaseViewModel
    {
        private readonly IRunService _runService;

        public ObservableCollection<VehicleRouteViewModel> AllRoutes { get; }
        public ICollectionView FilteredRoutes { get; }

        private VehicleRouteViewModel _selectedRoute;
        public VehicleRouteViewModel SelectedRoute
        {
            get => _selectedRoute;
            set { _selectedRoute = value; OnPropertyChanged(); }
        }

        private bool _showOnlyActive = false;
        public bool ShowOnlyActive
        {
            get => _showOnlyActive;
            set { _showOnlyActive = value; OnPropertyChanged(); FilteredRoutes.Refresh(); }
        }

        public ObservableCollection<VehicleGroup> VehicleGroups { get; }
        private VehicleGroup _selectedVehicleGroup;
        public VehicleGroup SelectedVehicleGroup
        {
            get => _selectedVehicleGroup;
            set { _selectedVehicleGroup = value; OnPropertyChanged(); FilteredRoutes.Refresh(); }
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand LoadDataCommand { get; }


        public RunsViewModel( IRunService runService)
        {
            _runService = runService;
            AllRoutes = new ObservableCollection<VehicleRouteViewModel>();
            VehicleGroups = new ObservableCollection<VehicleGroup>();

            FilteredRoutes = CollectionViewSource.GetDefaultView(AllRoutes);
            FilteredRoutes.Filter = FilterPredicate;

            AddCommand = new RelayCommandObject(param => AddRoute());
            EditCommand = new RelayCommandObject(param => EditRoute(), param => SelectedRoute != null);
            DeleteCommand = new RelayCommandObject(param => DeleteRoute(), param => SelectedRoute != null);
            LoadDataCommand = new RelayCommandObject(async param => await LoadData());

            LoadDataCommand.Execute(null);
        }

        private async Task LoadData()
        {
            VehicleGroupService _vehicleGroupService = new VehicleGroupService();
            //var groups = await _dataService.GetVehicleGroupsAsync();
            var groups = await _vehicleGroupService.GetGroupsAsync();
            VehicleGroups.Clear();
            VehicleGroups.Add(new VehicleGroup { Id = 0, Name = "Todos los Grupos" }); // Opción para deseleccionar filtro
            foreach (var group in groups)
            {
                VehicleGroups.Add(group);
            }

            RunService _runService = new RunService();   
            //var routes = await _dataService.GetVehicleRoutesAsync();
            var routes = await _runService.GetAllAsync();
            AllRoutes.Clear();
            foreach (var route in routes)
            {
                AllRoutes.Add(new VehicleRouteViewModel(route));
            }
        }

        private void AddRoute()
        {
            var newRoute = new VehicleRoute(); // Crear una ruta vacía por defecto
            var editorViewModel = new VehicleRouteEditorViewModel(newRoute, _runService);

            var editorWindow = new VehicleRouteEditorWindow
            {
                DataContext = editorViewModel,
                Owner = Application.Current.MainWindow
            };

            if (editorWindow.ShowDialog() == true)
            {
                // La ventana se cerró con "Guardar"
                var savedRouteVM = new VehicleRouteViewModel(newRoute);
                AllRoutes.Add(savedRouteVM);
                SelectedRoute = savedRouteVM;                          
            }
        }

        private void EditRoute()
        {
            if (SelectedRoute == null) return;

            // Pasamos una copia para no modificar el original hasta guardar
            var routeToEdit = SelectedRoute.Model;
            var editorViewModel = new VehicleRouteEditorViewModel(routeToEdit, _runService);

            var editorWindow = new VehicleRouteEditorWindow
            {
                DataContext = editorViewModel,
                Owner = Application.Current.MainWindow
            };

            if (editorWindow.ShowDialog() == true)
            {
                // Actualizar la vista del elemento existente
                SelectedRoute.Refresh();
                FilteredRoutes.Refresh();
            }
        }
        private async void DeleteRoute()
        {
            if (SelectedRoute == null) return;

            if (MessageBox.Show($"¿Está seguro de que desea eliminar la ruta '{SelectedRoute.Name}'?", "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    RunService _runService = new RunService();
                    await _runService.DeleteAsync(SelectedRoute.Id);
                    AllRoutes.Remove(SelectedRoute);
                }
                catch (ApiException ex)
                {
                    MessageBox.Show($"Error al eliminar: {ex.Message}", "Error de API", MessageBoxButton.OK, MessageBoxImage.Error);
                    //MessageBox.Show($"Error al eliminar: {ex.Message}\nDetalles: {ex.Details}", "Error de API", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private bool FilterPredicate(object item)
        {
            if (item is VehicleRouteViewModel route)
            {
                bool isActiveMatch = !ShowOnlyActive || route.IsActive;
                bool groupMatch = SelectedVehicleGroup == null || SelectedVehicleGroup.Id == 0 || route.VehicleGroupId == SelectedVehicleGroup.Id;
                return isActiveMatch && groupMatch;
            }
            return false;
        }

        #region Translation

        // Filters
        public string SelectVehicleGroupHint => LocalizationService.Instance["SelectVehicleGroupHint"];
        public string AddRunToolTip => LocalizationService.Instance["AddRunToolTip"]; // Add new Run

        public string EditRunToolTip => LocalizationService.Instance["EditRunToolTip"]; // Edit
        public string DeleteRunToolTip => LocalizationService.Instance["DeleteRunToolTip"]; // Cancel run by changing 'To Date' to today

        // Column headers
        public string ColumnHeaderName => LocalizationService.Instance["Name"];
        public string ColumnHeaderDescription => LocalizationService.Instance["Description"];
        public string ColumnHeaderSmartphoneLogin => LocalizationService.Instance["SmartphoneLogin"]; // Smartphone Login
        public string ColumnHeaderDriver => LocalizationService.Instance["Driver"]; // Driver / Conductor
        public string ColumnHeaderVehicle => LocalizationService.Instance["Vehicle"]; // Vehicle
        public string ColumnHeaderGarage => LocalizationService.Instance["Garage"]; // Garage
        public string ColumnHeaderFromDate => LocalizationService.Instance["FromDate"]; // From Date
        public string ColumnHeaderToDate => LocalizationService.Instance["ToDate"]; // To Date
        public string ColumnHeaderFromTime => LocalizationService.Instance["FromTime"]; // From Time
        public string ColumnHeaderToTime => LocalizationService.Instance["ToTime"]; // To Time 

        // Day of Week
        public string Sunday => LocalizationService.Instance["Sunday"];
        public string Monday => LocalizationService.Instance["Monday"];
        public string Tuesday => LocalizationService.Instance["Tuesday"];
        public string Wednesday => LocalizationService.Instance["Wednesday"];
        public string Thursday => LocalizationService.Instance["Thursday"];
        public string Friday => LocalizationService.Instance["Friday"];
        public string Saturday => LocalizationService.Instance["Saturday"];

        #endregion
    }
}