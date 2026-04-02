using ClosedXML.Excel;
using Meditrans.Client.Commands;
using Meditrans.Client.Models;
using Meditrans.Client.Models.Pdf;
using Meditrans.Client.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Meditrans.Client.ViewModels
{
    /// <summary>
    /// ViewModel for the ReportsView. Handles the logic for generating reports.
    /// </summary>
    public class ReportsViewModel : BaseViewModel
    {
        private readonly FundingSourceService _fundingSourceService;
        private readonly ScheduleService _scheduleService;

        private ObservableCollection<FundingSource> _allFundingSources;
        private FundingSource _selectedFundingSource;

        private DateTime _selectedDate = DateTime.Today;
        private string _generateButtonText = "Generate Production Report";
        private bool _isGenerating;

        private readonly GpsService _gpsService;
        private readonly RunService _vehicleRouteService;
        private ObservableCollection<VehicleRoute> _allVehicleRoutes;
        private VehicleRoute _selectedVehicleRoute;
        private string _generateGpsButtonText = "Generate GPS Report";
        private bool _isGeneratingGps;

        private string _generateTrip2ButtonText = "Generate Trip2 PDF";
        private bool _isGeneratingTrip2;

        private DateTime _startDate = DateTime.Today;
        private DateTime _endDate = DateTime.Today;
        private string _generateAviataButtonText = "Generate Aviata Report";
        private bool _isGeneratingAviata;

        public ICommand GenerateProductionReportCommand { get; }
        public ICommand GenerateGpsReportCommand { get; }
        public ICommand GenerateTrip2PdfCommand { get; }
        public ICommand GenerateAviataReportCommand { get; }

        public string GenerateTrip2ButtonText
        {
            get => _generateTrip2ButtonText;
            set { _generateTrip2ButtonText = value; OnPropertyChanged(); }
        }
        public DateTime StartDate { get => _startDate; set { _startDate = value; OnPropertyChanged(); } }
        public DateTime EndDate { get => _endDate; set { _endDate = value; OnPropertyChanged(); } }
        public string GenerateAviataButtonText { get => _generateAviataButtonText; set { _generateAviataButtonText = value; OnPropertyChanged(); } }

        public ReportsViewModel()
        {
            _fundingSourceService = new FundingSourceService();
            _scheduleService = new ScheduleService();
            _gpsService = new GpsService();
            _vehicleRouteService = new RunService();

            // We pass the async method to the command and manage the execution state.
            GenerateProductionReportCommand = new RelayCommandObject(async (obj) => await GenerateProductionReport(obj), (obj) => !_isGenerating);
            GenerateGpsReportCommand = new RelayCommandObject(async (o) => await GenerateGpsReport(o), (o) => !_isGeneratingGps && SelectedVehicleRoute != null);
            GenerateTrip2PdfCommand = new RelayCommandObject(async (o) => await GenerateTrip2Report(o), (o) => !_isGeneratingTrip2);
            GenerateAviataReportCommand = new RelayCommandObject(async (o) => await GenerateAviataReport(o), (o) => !_isGeneratingAviata);

            // Load initial data
            LoadVehicleRoutes();
            LoadFundingSources();
        }

        private async void LoadFundingSources()
        {
            try
            {
                var sources = await _fundingSourceService.GetFundingSourcesAsync(false); // Get only active sources

                // --- Create a special "All" item ---
                // This is a placeholder for the UI. Its ID helps us know when to send 'null' to the API.
                var allSourcesPlaceholder = new FundingSource { Id = -1, Name = "[ All Funding Sources ]" };

                sources.Insert(0, allSourcesPlaceholder); // Add it to the top of the list

                AllFundingSources = new ObservableCollection<FundingSource>(sources);
                SelectedFundingSource = allSourcesPlaceholder; // Set it as the default selection
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load funding sources: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public ObservableCollection<FundingSource> AllFundingSources
        {
            get => _allFundingSources;
            set { _allFundingSources = value; OnPropertyChanged(); }
        }

        public FundingSource SelectedFundingSource
        {
            get => _selectedFundingSource;
            set { _selectedFundingSource = value; OnPropertyChanged(); }
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged();
            }
        }

        public string GenerateButtonText
        {
            get => _generateButtonText;
            set
            {
                _generateButtonText = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<VehicleRoute> AllVehicleRoutes
        {
            get => _allVehicleRoutes;
            set { _allVehicleRoutes = value; OnPropertyChanged(); }
        }

        public VehicleRoute SelectedVehicleRoute
        {
            get => _selectedVehicleRoute;
            set { _selectedVehicleRoute = value; OnPropertyChanged(); }
        }

        public string GenerateGpsButtonText
        {
            get => _generateGpsButtonText;
            set { _generateGpsButtonText = value; OnPropertyChanged(); }
        }

        private async void LoadVehicleRoutes()
        {
            try
            {
                var routes = await _vehicleRouteService.GetAllAsync();
                AllVehicleRoutes = new ObservableCollection<VehicleRoute>(routes);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load vehicle routes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task GenerateProductionReport(object obj)
        {
            _isGenerating = true;
            GenerateButtonText = "Generating...";
            CommandManager.InvalidateRequerySuggested();

            try
            {
                int? fundingSourceId = (SelectedFundingSource != null && SelectedFundingSource.Id != -1)
                    ? SelectedFundingSource.Id : (int?)null;

                var reportData = await _scheduleService.GetProductionReportDataAsync(SelectedDate, fundingSourceId);

                if (reportData == null || reportData.Count == 0)
                {
                    MessageBox.Show("No production data found.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Workbook|*.xlsx",
                    FileName = $"ProductionReport_{SelectedDate:yyyyMMdd}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Production Report");
                       
                        var headers = new string[] {
                    "Date", "Req Pickup", "Appointment", "Patient", "Pickup Address",
                    "Dropoff Address", "Space", "Charge", "Paid", "Pickup Comment",
                    "Dropoff Comment", "Type", "Pickup Phone", "Dropoff Phone", "Authorization",
                    "Funding Source", "Distance", "Run", "Driver", "Pickup Arrive",
                    "Pickup Perform", "Dropoff Arrive", "Dropoff Perform", "Will Call", "Canceled",
                    "VIN", "Pickup Odometer", "Dropoff Odometer", "Will Call Time", "Vehicle",
                    "Vehicle Plate", "Trip Id", "Pickup GPS Arrive Distance", "Dropoff GPS Arrive Distance", "Pickup City",
                    "Pickup State", "Pickup Zip", "Dropoff City", "Dropoff State", "Dropoff Zip",
                    "Patient Address", "DOB", "Driver No-Show Reason", "Pickup Lat", "Pickup Lon",
                    "Dropoff Lat", "Dropoff Lon", "Created"
                };

                        for (int i = 0; i < headers.Length; i++)
                        {
                            worksheet.Cell(1, i + 1).Value = headers[i];
                        }
                      
                        for (int i = 0; i < reportData.Count; i++)
                        {
                            var d = reportData[i];
                            var r = i + 2;

                            worksheet.Cell(r, 1).Value = d.Date.ToShortDateString();
                            worksheet.Cell(r, 2).Value = d.ReqPickup?.ToString(@"hh\:mm") ?? "";
                            worksheet.Cell(r, 3).Value = d.Appointment?.ToString(@"hh\:mm") ?? "";
                            worksheet.Cell(r, 4).Value = d.Patient;
                            worksheet.Cell(r, 5).Value = d.PickupAddress;
                            worksheet.Cell(r, 6).Value = d.DropoffAddress;
                            worksheet.Cell(r, 7).Value = d.Space;
                            worksheet.Cell(r, 8).Value = d.Charge;
                            worksheet.Cell(r, 9).Value = d.Paid;
                            worksheet.Cell(r, 10).Value = d.PickupComment;
                            worksheet.Cell(r, 11).Value = d.DropoffComment;
                            worksheet.Cell(r, 12).Value = d.Type;
                            worksheet.Cell(r, 13).Value = d.PickupPhone;
                            worksheet.Cell(r, 14).Value = d.DropoffPhone;
                            worksheet.Cell(r, 15).Value = d.Authorization;
                            worksheet.Cell(r, 16).Value = d.FundingSource;
                            worksheet.Cell(r, 17).Value = d.Distance;
                            worksheet.Cell(r, 18).Value = d.Run;
                            worksheet.Cell(r, 19).Value = d.Driver;
                            worksheet.Cell(r, 20).Value = d.PickupArrive?.ToString(@"hh\:mm") ?? "";
                            worksheet.Cell(r, 21).Value = d.PickupPerform?.ToString(@"hh\:mm") ?? "";
                            worksheet.Cell(r, 22).Value = d.DropoffArrive?.ToString(@"hh\:mm") ?? "";
                            worksheet.Cell(r, 23).Value = d.DropoffPerform?.ToString(@"hh\:mm") ?? "";
                            worksheet.Cell(r, 24).Value = d.WillCall ? "Yes" : "No";
                            worksheet.Cell(r, 25).Value = d.Canceled ? "Yes" : "No";
                            worksheet.Cell(r, 26).Value = d.VIN;
                            worksheet.Cell(r, 27).Value = d.PickupOdometer;
                            worksheet.Cell(r, 28).Value = d.DropoffOdometer;
                            worksheet.Cell(r, 29).Value = d.WillCallTime?.ToString(@"hh\:mm") ?? "";
                            worksheet.Cell(r, 30).Value = d.Vehicle;
                            worksheet.Cell(r, 31).Value = d.VehiclePlate;
                            worksheet.Cell(r, 32).Value = d.TripId;
                            worksheet.Cell(r, 33).Value = d.PickupGpsArriveDistance;
                            worksheet.Cell(r, 34).Value = d.DropoffGpsArriveDistance;
                            worksheet.Cell(r, 35).Value = d.PickupCity;
                            worksheet.Cell(r, 36).Value = d.PickupState;
                            worksheet.Cell(r, 37).Value = d.PickupZip;
                            worksheet.Cell(r, 38).Value = d.DropoffCity;
                            worksheet.Cell(r, 39).Value = d.DropoffState;
                            worksheet.Cell(r, 40).Value = d.DropoffZip;
                            worksheet.Cell(r, 41).Value = d.PatientAddress;
                            worksheet.Cell(r, 42).Value = d.DOB?.ToShortDateString() ?? "";
                            worksheet.Cell(r, 43).Value = d.DriverNoShowReason;
                            worksheet.Cell(r, 44).Value = d.PickupLat;
                            worksheet.Cell(r, 45).Value = d.PickupLon;
                            worksheet.Cell(r, 46).Value = d.DropoffLat;
                            worksheet.Cell(r, 47).Value = d.DropoffLon;
                            worksheet.Cell(r, 48).Value = d.Created.ToString("g");
                        }

                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(saveFileDialog.FileName);
                        MessageBox.Show("Report generated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isGenerating = false;
                GenerateButtonText = "Generate Production Report";
                CommandManager.InvalidateRequerySuggested();
            }
        }
        private async Task GenerateProductionReportOld(object obj)
        {
            _isGenerating = true;
            GenerateButtonText = "Generating...";
            // This is important to update the UI, especially the button's IsEnabled state
            CommandManager.InvalidateRequerySuggested();

            try
            {
                // --- Determine the ID to send to the API ---
                // If the user selected our "[ All ]" placeholder, we send null.
                // Otherwise, we send the selected ID.
                int? fundingSourceId = (SelectedFundingSource != null && SelectedFundingSource.Id != -1)
                    ? SelectedFundingSource.Id
                    : (int?)null;

                // Fetch data from the API
                var reportData = await _scheduleService.GetProductionReportDataAsync(SelectedDate, fundingSourceId);

                if (reportData == null || reportData.Count == 0)
                {
                    MessageBox.Show("No production data found for the selected date.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Show SaveFileDialog to the user
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Workbook|*.xlsx",
                    Title = "Save Production Report",
                    FileName = $"ProductionReport_{SelectedDate:yyyyMMdd}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Create and populate the Excel file
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Production Report");

                        var headers = new List<string>
                        {
                            "Date", "Authorization", "Req Pickup", "Appointment", "Patient",
                            "Pickup City", "Run", "Space", "Pickup Arrive", "Dropoff Arrive"
                        };

                        for (int i = 0; i < headers.Count; i++)
                        {
                            worksheet.Cell(1, i + 1).Value = headers[i];
                        }

                        // Populate with the data from the API
                        for (int i = 0; i < reportData.Count; i++)
                        {
                            var rowData = reportData[i];
                            var row = i + 2; // Start from the second row
                            worksheet.Cell(row, 1).Value = rowData.Date;
                            worksheet.Cell(row, 2).Value = rowData.Authorization;
                            worksheet.Cell(row, 3).Value = rowData.ReqPickup;
                            worksheet.Cell(row, 4).Value = rowData.Appointment;
                            worksheet.Cell(row, 5).Value = rowData.Patient;
                            worksheet.Cell(row, 6).Value = rowData.PickupCity;
                            worksheet.Cell(row, 7).Value = rowData.Run;
                            worksheet.Cell(row, 8).Value = rowData.Space;
                            worksheet.Cell(row, 9).Value = rowData.PickupArrive;
                            worksheet.Cell(row, 10).Value = rowData.DropoffArrive;
                        }

                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(saveFileDialog.FileName);

                        MessageBox.Show("Report generated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while generating the report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isGenerating = false;
                GenerateButtonText = "Generate Production Report";
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async Task GenerateGpsReport(object obj)
        {
            if (SelectedVehicleRoute == null)
            {
                MessageBox.Show("Please select a vehicle route.", "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _isGeneratingGps = true;
            GenerateGpsButtonText = "Generating...";
            CommandManager.InvalidateRequerySuggested();

            try
            {
                // 1. Fetch data from the API
                var reportData = await _gpsService.GetGpsHistoryAsync(SelectedVehicleRoute.Id, SelectedDate);

                if (reportData == null || reportData.Count == 0)
                {
                    MessageBox.Show("No GPS data found for the selected date and route.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 2. Show SaveFileDialog
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Workbook|*.xlsx",
                    Title = "Save GPS Report",
                    FileName = $"GpsReport_{SelectedVehicleRoute.Name.Replace(" ", "")}_{SelectedDate:yyyyMMdd}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // 3. Create and populate the Excel file
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("GPS History");

                        var headers = new List<string> { "Date Time", "Speed", "Address", "Direction", "Latitude", "Longitude" };
                        for (int i = 0; i < headers.Count; i++)
                        {
                            worksheet.Cell(1, i + 1).Value = headers[i];
                        }

                        for (int i = 0; i < reportData.Count; i++)
                        {
                            var rowData = reportData[i];
                            var row = i + 2;
                            worksheet.Cell(row, 1).Value = rowData.DateTime;
                            worksheet.Cell(row, 2).Value = rowData.Speed;
                            worksheet.Cell(row, 3).Value = rowData.Address;
                            worksheet.Cell(row, 4).Value = rowData.Direction;
                            worksheet.Cell(row, 5).Value = rowData.Latitude;
                            worksheet.Cell(row, 6).Value = rowData.Longitude;
                        }

                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(saveFileDialog.FileName);

                        MessageBox.Show("GPS report generated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while generating the GPS report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isGeneratingGps = false;
                GenerateGpsButtonText = "Generate GPS Report";
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async Task GenerateAviataReport(object obj)
        {
            _isGeneratingAviata = true;
            GenerateAviataButtonText = "Generating...";
            try
            {
                // Debes implementar GetAviataReportDataAsync en tu ScheduleService del cliente que llame a la nueva ruta de la API
                var reportData = await _scheduleService.GetAviataReportDataAsync(StartDate, EndDate);

                if (!reportData.Any())
                {
                    MessageBox.Show("No data found."); return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF Document|*.pdf",
                    FileName = $"Aviata_{StartDate:MM-dd-yyyy}_{EndDate:MM-dd-yyyy}.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    QuestPDF.Settings.License = LicenseType.Community;
                    // Agrupar por Cliente
                    var groupedByClient = reportData.GroupBy(x => x.Patient).ToList();
                    var document = new AviataDocument(groupedByClient);
                    document.GeneratePdf(saveFileDialog.FileName);
                    MessageBox.Show("Report generated!");
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
            finally
            {
                _isGeneratingAviata = false;
                GenerateAviataButtonText = "Generate Aviata Report";
            }
        }
        private async Task GenerateTrip2Report(object obj)
        {
            _isGeneratingTrip2 = true;
            GenerateTrip2ButtonText = "Generating PDF...";
            CommandManager.InvalidateRequerySuggested();

            try
            {                
                var reportData = await _scheduleService.GetProductionReportDataAsync(SelectedDate, null);

                if (reportData == null || !reportData.Any())
                {
                    MessageBox.Show("No data found for this date.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
               
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF Document|*.pdf",
                    FileName = $"Trip2_{SelectedDate:MM-dd-yyyy}.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Agrupar datos por Driver (Cada driver inicia en página nueva)
                    var groupedByDriver = reportData.GroupBy(d => d.Driver ?? "Unknown Driver").ToList();

                    // Generar el PDF usando QuestPDF                  
                    QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
                    var document = new Trip2Document(groupedByDriver, SelectedDate);
                    document.GeneratePdf(saveFileDialog.FileName);

                    MessageBox.Show("PDF Report generated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isGeneratingTrip2 = false;
                GenerateTrip2ButtonText = "Generate Trip2 PDF";
                CommandManager.InvalidateRequerySuggested();
            }
        }


    } // end class


}
