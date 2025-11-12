using ClosedXML.Excel;
using Meditrans.Client.Commands;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Meditrans.Client.ViewModels
{
    /// <summary>
    /// ViewModel for the ReportsView. Handles the logic for generating reports.
    /// </summary>
    public class ReportsViewModel : BaseViewModel
    {
        public ICommand GenerateProductionReportCommand { get; }

        public ReportsViewModel()
        {
            //AddCommand = new RelayCommandObject(param => AddRoute());
            GenerateProductionReportCommand = new RelayCommandObject(GenerateProductionReport);
        }

        /// <summary>
        /// Generates and saves the Production Report as an Excel file.
        /// </summary>
        private void GenerateProductionReport(object obj)
        {
            // Use SaveFileDialog to let the user choose where to save the file
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook|*.xlsx",
                Title = "Save Production Report",
                FileName = $"ProductionReport_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Create a new Excel workbook
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Production Report");

                        // Define the headers
                        var headers = new List<string>
                        {
                            "Date", "Authorization", "Req Pickup", "Appointment", "Patient",
                            "Pickup City", "Run", "Space", "Pickup Arrive", "Dropoff Arrive"
                        };

                        // Add headers to the first row
                        for (int i = 0; i < headers.Count; i++)
                        {
                            worksheet.Cell(1, i + 1).Value = headers[i];
                        }

                        // --- TODO: Replace this with your actual data retrieval logic ---
                        // For demonstration, we'll add some sample data.
                        var sampleData = GetSampleProductionData();

                        // Populate the worksheet with data starting from the second row
                        for (int rowIndex = 0; rowIndex < sampleData.Count; rowIndex++)
                        {
                            var rowData = sampleData[rowIndex];
                            worksheet.Cell(rowIndex + 2, 1).Value = rowData.Date;
                            worksheet.Cell(rowIndex + 2, 2).Value = rowData.Authorization;
                            worksheet.Cell(rowIndex + 2, 3).Value = rowData.ReqPickup;
                            worksheet.Cell(rowIndex + 2, 4).Value = rowData.Appointment;
                            worksheet.Cell(rowIndex + 2, 5).Value = rowData.Patient;
                            worksheet.Cell(rowIndex + 2, 6).Value = rowData.PickupCity;
                            worksheet.Cell(rowIndex + 2, 7).Value = rowData.Run;
                            worksheet.Cell(rowIndex + 2, 8).Value = rowData.Space;
                            worksheet.Cell(rowIndex + 2, 9).Value = rowData.PickupArrive;
                            worksheet.Cell(rowIndex + 2, 10).Value = rowData.DropoffArrive;
                        }
                        // ----------------------------------------------------------------

                        // Autofit columns for better readability
                        worksheet.Columns().AdjustToContents();

                        // Save the workbook
                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    // Optionally, show a success message to the user
                    System.Windows.MessageBox.Show("Report generated successfully!", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                }
                catch (Exception ex)
                {
                    // Handle potential errors during file generation/saving
                    System.Windows.MessageBox.Show($"An error occurred while generating the report: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Generates a list of sample production data. 
        /// Replace this method with your actual data access logic.
        /// </summary>
        private List<ProductionData> GetSampleProductionData()
        {
            return new List<ProductionData>
            {
                new ProductionData { Date = "2025-11-12", Authorization = "AUTH123", ReqPickup = "10:00 AM", Appointment = "11:00 AM", Patient = "John Doe", PickupCity = "New York", Run = "A1", Space = "S1", PickupArrive = "10:05 AM", DropoffArrive = "10:55 AM" },
                new ProductionData { Date = "2025-11-12", Authorization = "AUTH456", ReqPickup = "10:30 AM", Appointment = "11:30 AM", Patient = "Jane Smith", PickupCity = "New York", Run = "A1", Space = "S2", PickupArrive = "10:35 AM", DropoffArrive = "11:25 AM" },
                new ProductionData { Date = "2025-11-12", Authorization = "AUTH789", ReqPickup = "11:00 AM", Appointment = "12:00 PM", Patient = "Peter Jones", PickupCity = "Brooklyn", Run = "B2", Space = "S1", PickupArrive = "11:05 AM", DropoffArrive = "11:50 AM" }
            };
        }


    }

    /// <summary>
    /// A simple data class to hold production report data.
    /// This should be replaced by your actual data model.
    /// </summary>
    public class ProductionData
    {
        public string Date { get; set; }
        public string Authorization { get; set; }
        public string ReqPickup { get; set; }
        public string Appointment { get; set; }
        public string Patient { get; set; }
        public string PickupCity { get; set; }
        public string Run { get; set; }
        public string Space { get; set; }
        public string PickupArrive { get; set; }
        public string DropoffArrive { get; set; }
    }
}
