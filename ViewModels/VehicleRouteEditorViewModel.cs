using Meditrans.Client.Commands;
using Meditrans.Client.Models;
using Meditrans.Client.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Meditrans.Client.ViewModels
{
    public class VehicleRouteEditorViewModel : BaseViewModel
    {
        //private readonly IDataService _dataService;
        private readonly VehicleRoute _originalRoute;

        // Propiedad para enlazar a todos los controles del formulario.
        // Trabajamos sobre esta copia y solo guardamos si el usuario confirma.
        public VehicleRoute Route { get; private set; }

        // Colecciones para los ComboBoxes y Listas en los Tabs
        public ObservableCollection<User> AllDrivers { get; }
        public ObservableCollection<Vehicle> AllVehicles { get; }
        public ObservableCollection<SelectableFundingSourceViewModel> AllFundingSources { get; }
        public ObservableCollection<DailyAvailabilityViewModel> DailyAvailabilities { get; }
        public ObservableCollection<RouteSuspension> Suspensions { get; }

        // Comandos para los botones Guardar y Cancelar
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand AddSuspensionCommand { get; }
        public ICommand RemoveSuspensionCommand { get; }


        // Evento para notificar a la vista (la ventana) que debe cerrarse
        public event EventHandler<bool?> RequestClose;

        public VehicleRouteEditorViewModel(VehicleRoute route/*, IDataService dataService*/)
        {
            //_dataService = dataService;
            _originalRoute = route; // Guardamos el original por si hay que revertir

            // Creamos una copia profunda (simplificada aquí) para no editar el objeto original directamente.
            // Para una app real, considera usar una librería como AutoMapper o implementar ICloneable.
            Route = CloneRoute(route);

            // Inicializar colecciones
            AllDrivers = new ObservableCollection<User>();
            AllVehicles = new ObservableCollection<Vehicle>();
            AllFundingSources = new ObservableCollection<SelectableFundingSourceViewModel>();
            DailyAvailabilities = new ObservableCollection<DailyAvailabilityViewModel>();
            Suspensions = new ObservableCollection<RouteSuspension>(Route.Suspensions);

            // Inicializar comandos
            SaveCommand = new RelayCommandObject(async param => await SaveAsync(), param => CanSave());
            CancelCommand = new RelayCommandObject(param => Cancel());
            AddSuspensionCommand = new RelayCommandObject(param => AddSuspension());
            RemoveSuspensionCommand = new RelayCommandObject(param => RemoveSuspension(param as RouteSuspension), param => param != null);


            // Cargar datos asincrónicamente
            LoadDependenciesAsync();
        }

        private async Task LoadDependenciesAsync()
        {
            // Cargar conductores y vehículos
            UserService _userService = new UserService();
            //var drivers = await _userService.GetDriversAsync();
            var drivers = await _userService.GetAllAsync();
            drivers.ForEach(d => AllDrivers.Add(d));
            VehicleService _vehicleService = new VehicleService();
            var vehicles = await _vehicleService.GetVehiclesAsync();
            vehicles.ForEach(v => AllVehicles.Add(v));

            // Asegurarse de que el conductor y vehículo seleccionados en la ruta
            // estén correctamente asignados en los ComboBoxes.
            if (Route.DriverId > 0)
            {
                Route.Driver = AllDrivers.FirstOrDefault(d => d.Id == Route.DriverId);
            }
            if (Route.VehicleId > 0)
            {
                Route.Vehicle = AllVehicles.FirstOrDefault(v => v.Id == Route.VehicleId);
            }
            OnPropertyChanged(nameof(Route)); // Notificar que la propiedad Route ha sido actualizada

            // Cargar y preparar la lista de Fuentes de Financiamiento
            FundingSourceService _fundingSourceService = new FundingSourceService();           
            var allFundingSources = await _fundingSourceService.GetFundingSourcesAsync();
            var exclusiveIds = new HashSet<int>(Route.FundingSources.Select(fs => fs.FundingSourceId));

            foreach (var fs in allFundingSources)
            {
                AllFundingSources.Add(new SelectableFundingSourceViewModel(fs)
                {
                    IsSelected = exclusiveIds.Contains(fs.Id)
                });
            }
            // Si es una ruta nueva y no tiene fuentes exclusivas, por defecto no se selecciona ninguna.
            // La lógica de negocio indica que "ninguna seleccionada" significa "todas son válidas".

            // Preparar la lista de Disponibilidad Diaria
            var daysOfWeek = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>();
            foreach (var day in daysOfWeek)
            {
                var existingAvailability = Route.Availabilities.FirstOrDefault(a => a.DayOfWeek == day);
                var dailyVM = new DailyAvailabilityViewModel(day);

                if (existingAvailability != null)
                {
                    // Si ya existe una configuración para este día, usarla
                    dailyVM.IsActive = existingAvailability.IsActive;
                    dailyVM.StartTime = existingAvailability.StartTime;
                    dailyVM.EndTime = existingAvailability.EndTime;
                }
                else
                {
                    // Si no existe, usar los horarios generales de la ruta
                    dailyVM.StartTime = Route.FromTime;
                    dailyVM.EndTime = Route.ToTime;
                }
                DailyAvailabilities.Add(dailyVM);
            }
        }

        private bool CanSave()
        {
            // Validación simple. Puedes añadir más reglas aquí.
            return !string.IsNullOrWhiteSpace(Route.Name) &&
                   Route.DriverId > 0 &&
                   Route.VehicleId > 0 &&
                   Route.FromDate != default &&
                   Route.FromTime != default &&
                   Route.ToTime != default &&
                   Route.ToTime > Route.FromTime;
        }

        private async Task SaveAsync()
        {
            if (!CanSave())
            {
                MessageBox.Show("Por favor, complete todos los campos requeridos y asegúrese de que los horarios son válidos.", "Datos incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 1. Actualizar la colección de Disponibilidad Diaria en el modelo
            Route.Availabilities.Clear();
            foreach (var dailyVM in DailyAvailabilities)
            {
                // Solo guardamos la configuración si es diferente del comportamiento por defecto
                // (activo y con los horarios generales de la ruta) para mantener la data limpia.
                // Opcional: Podrías decidir guardar siempre las 7 entradas para ser más explícito.
                bool isDefault = dailyVM.IsActive && dailyVM.StartTime == Route.FromTime && dailyVM.EndTime == Route.ToTime;
                if (!isDefault)
                {
                    Route.Availabilities.Add(new RouteAvailability
                    {
                        VehicleRouteId = Route.Id,
                        DayOfWeek = dailyVM.DayOfWeek,
                        IsActive = dailyVM.IsActive,
                        StartTime = dailyVM.StartTime,
                        EndTime = dailyVM.EndTime
                    });
                }
            }

            // 2. Actualizar la colección de Fuentes de Financiamiento en el modelo
            Route.FundingSources.Clear();
            var selectedFundingSources = AllFundingSources.Where(fsvm => fsvm.IsSelected);
            foreach (var fsvm in selectedFundingSources)
            {
                Route.FundingSources.Add(new RouteFundingSource
                {
                    VehicleRouteId = Route.Id,
                    FundingSourceId = fsvm.FundingSource.Id
                });
            }

            // 3. Actualizar Suspensiones
            Route.Suspensions = new List<RouteSuspension>(Suspensions);

            // 4. Persistir los cambios usando el servicio de datos
            //var savedRoute = await _dataService.SaveVehicleRouteAsync(Route); // OJO

            // 5. Actualizar el objeto original con los datos guardados
            // Esto es importante para que la vista principal refleje los cambios.
            //UpdateOriginalRoute(savedRoute); // OJO

            // 6. Cerrar la ventana con un resultado de éxito
            RequestClose?.Invoke(this, true);
        }

        private void Cancel()
        {
            // Simplemente cierra la ventana sin guardar
            RequestClose?.Invoke(this, false);
        }

        private void AddSuspension()
        {
            Suspensions.Add(new RouteSuspension
            {
                VehicleRouteId = Route.Id,
                SuspensionStart = DateTime.Today,
                SuspensionEnd = DateTime.Today.AddDays(1)
            });
        }

        private void RemoveSuspension(RouteSuspension suspension)
        {
            if (suspension != null)
            {
                Suspensions.Remove(suspension);
            }
        }

        #region Helper Methods

        private VehicleRoute CloneRoute(VehicleRoute source)
        {
            // Esto es una clonación superficial. Para este caso es suficiente porque los tipos primitivos se copian.
            // Las colecciones se manejarán por separado.
            var clone = new VehicleRoute
            {
                Id = source.Id,
                Name = source.Name,
                Description = source.Description,
                DriverId = source.DriverId,
                VehicleId = source.VehicleId,
                Garage = source.Garage,
                GarageLatitude = source.GarageLatitude,
                GarageLongitude = source.GarageLongitude,
                SmartphoneLogin = source.SmartphoneLogin,
                FromDate = source.Id == 0 ? DateTime.Today : source.FromDate, // Valor por defecto si es nuevo
                ToDate = source.ToDate,
                FromTime = source.Id == 0 ? new TimeSpan(8, 0, 0) : source.FromTime,
                ToTime = source.Id == 0 ? new TimeSpan(18, 0, 0) : source.ToTime,
                Availabilities = new List<RouteAvailability>(source.Availabilities ?? new List<RouteAvailability>()),
                FundingSources = new List<RouteFundingSource>(source.FundingSources ?? new List<RouteFundingSource>()),
                Suspensions = new List<RouteSuspension>(source.Suspensions ?? new List<RouteSuspension>())
            };
            return clone;
        }

        private void UpdateOriginalRoute(VehicleRoute savedRoute)
        {
            // Copia las propiedades del objeto guardado (que puede tener un nuevo Id) al objeto original.
            _originalRoute.Id = savedRoute.Id;
            _originalRoute.Name = savedRoute.Name;
            _originalRoute.Description = savedRoute.Description;
            _originalRoute.DriverId = savedRoute.DriverId;
            _originalRoute.Driver = savedRoute.Driver;
            _originalRoute.VehicleId = savedRoute.VehicleId;
            _originalRoute.Vehicle = savedRoute.Vehicle;
            _originalRoute.Garage = savedRoute.Garage;
            _originalRoute.GarageLatitude = savedRoute.GarageLatitude;
            _originalRoute.GarageLongitude = savedRoute.GarageLongitude;
            _originalRoute.SmartphoneLogin = savedRoute.SmartphoneLogin;
            _originalRoute.FromDate = savedRoute.FromDate;
            _originalRoute.ToDate = savedRoute.ToDate;
            _originalRoute.FromTime = savedRoute.FromTime;
            _originalRoute.ToTime = savedRoute.ToTime;
            _originalRoute.Availabilities = savedRoute.Availabilities;
            _originalRoute.FundingSources = savedRoute.FundingSources;
            _originalRoute.Suspensions = savedRoute.Suspensions;
        }

        #endregion
    }
}