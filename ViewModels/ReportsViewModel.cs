using ClosedXML.Excel;
using Meditrans.Client.Commands;

using Meditrans.Client.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Meditrans.Client.ViewModels
{
    /// <summary>
    /// ViewModel for the ReportsView. Handles the logic for generating reports.
    /// </summary>
    public class ReportsViewModel : BaseViewModel
    {
        private readonly ScheduleService _scheduleService;
        private DateTime _selectedDate = DateTime.Today;
        private string _generateButtonText = "Generate Production Report";
        private bool _isGenerating;
        public ICommand GenerateProductionReportCommand { get; }

      

        


        public ReportsViewModel()
        {
            _scheduleService = new ScheduleService();
            // We pass the async method to the command and manage the execution state.
            GenerateProductionReportCommand = new RelayCommandObject(async (obj) => await GenerateProductionReport(obj), (obj) => !_isGenerating);
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

        private async Task GenerateProductionReport(object obj)
        {
            _isGenerating = true;
            GenerateButtonText = "Generating...";
            // This is important to update the UI, especially the button's IsEnabled state
            CommandManager.InvalidateRequerySuggested();

            try
            {
                // Fetch data from the API
                var reportData = await _scheduleService.GetProductionReportDataAsync(SelectedDate);

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

       
    }


}
