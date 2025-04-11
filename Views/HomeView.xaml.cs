using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Meditrans.Client.ViewModels;
using Meditrans.Client.Models;
using System.Collections.ObjectModel;
using Microsoft.Web.WebView2.Core;
using System.Text.Json;
using Meditrans.Client.Services;
using System.Configuration;

namespace Meditrans.Client.Views
{
    /// <summary>
    /// Lógica de interacción para HomeView.xaml
    /// </summary>
    public partial class HomeView : UserControl
    {
        public HomeViewModel ViewModel { get; set; }
        public HomeView()
        {
            InitializeComponent();
            ViewModel = new HomeViewModel();
            DataContext = ViewModel;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            InitializeData();

            // It doesn't work properly, it never loads the map.
            // For this reason, WebView_Loaded() was used.
            //MapaWebView.CoreWebView2InitializationCompleted += MapaWebView_CoreWebView2InitializationCompleted;
        }
        private async void InitializeData()
        {
            ViewModel.LoadTripsFromApi();
            //InitializeWebView();
            
        }

        private void MapaWebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                LoadMap();
            }
            else
            {
                MessageBox.Show("Failed to initialize WebView2.");
            }
        }

        private async void WebView_Loaded(object sender, RoutedEventArgs e)
        {        
            try
            {
                await MapaWebView.EnsureCoreWebView2Async();
                LoadMap();
                // MapaWebView.WebMessageReceived += MapaWebView_WebMessageReceived;
                // Suscribirse al mensaje desde JavaScript
                MapaWebView.CoreWebView2.WebMessageReceived += (s, args) =>
                {
                    try
                    {
                        var json = args.WebMessageAsJson;
                        dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

                        if (data.type == "routeInfo")
                        {
                            string eta = data.eta;
                            string distance = data.distance;


                            // Show on Label
                            Dispatcher.Invoke(() =>
                            {
                                TripInfoLabel.Content = $"ETA: {eta} | Distance: {distance}";
                            });
                            // Here you can display the message
                            //MessageBox.Show($"ETA: {eta}\nDistance: {distance}", "Trip Info");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error receiving message: " + ex.Message);
                    }
                };

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing WebView2: {ex.Message}");
            }

        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedTrip")
            {
                LoadMap();
            }
        }

        private void LoadMap()
        {
            var trip = ViewModel.SelectedTrip;

            if (trip == null || MapaWebView.CoreWebView2 == null)
                return;

            string apiKey = App.Configuration["GoogleMaps:ApiKey"];
            //MessageBox.Show($"La API Key es: {apiKey}");

            string htmlPath = File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "googlemap.html"));
            htmlPath = htmlPath.Replace("{{API_KEY}}", apiKey);

            htmlPath = htmlPath.Replace("{LAT_ORIGEN}", trip.PickupLatitude.ToString())
                               .Replace("{LNG_ORIGEN}", trip.PickupLongitude.ToString())
                               .Replace("{LAT_DESTINO}", trip.DropoffLatitude.ToString())
                               .Replace("{ORIGEN}", trip.PickupAddress)
                               .Replace("{NOMBRE}", trip.PatientName)
                               .Replace("{LNG_DESTINO}", trip.DropoffLongitude.ToString());

            MapaWebView.NavigateToString(htmlPath);

            ShowETAInfo(trip);
        }

        public void aa ()
        {
            // Ruta del archivo HTML de plantilla
            string templatePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "googlemap.html");

            // Leer HTML original
            string htmlContent = File.ReadAllText(templatePath);

            // Leer la clave desde App.config
            string apiKey = ConfigurationManager.AppSettings["GoogleMapsApiKey"];

            // Reemplazar {{API_KEY}} por la real
            htmlContent = htmlContent.Replace("{{API_KEY}}", apiKey);

            // Crear archivo temporal con el HTML ya modificado
            string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "googlemap2.html");
            File.WriteAllText(tempPath, htmlContent);

            // Cargar en el WebView2

            MapaWebView.CoreWebView2.Navigate(tempPath);
        }
        public async void ShowETAInfo(Trip SelectedTrip) {

            GoogleMapsService googleMapsService = new GoogleMapsService();
            var routeInfo = await googleMapsService.GetRouteDurationsAndDistance(SelectedTrip.PickupLatitude, SelectedTrip.PickupLongitude, SelectedTrip.DropoffLatitude, SelectedTrip.DropoffLongitude);

            // Mostrar las duraciones y distancia en el Label
            Label etaLabel = new Label();
            etaLabel.Content = $"Duration without traffic: {routeInfo.noTraffic}\n" +
                              $"Duration with traffic: {routeInfo.withTraffic}\n" +
                              $"Duration (pessimistic): {routeInfo.pessimistic}\n" +
                              $"Duration (optimistic): {routeInfo.optimistic}\n" +
                              $"Distance: {routeInfo.distance}";

            Dispatcher.Invoke(() =>
            {
                ETAInfoLabel.Content = etaLabel.Content;
            });
        }

        private void MapaWebView_WebMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var json = e.WebMessageAsJson;
                var data = JsonSerializer.Deserialize<RouteInfoMessage>(json);

                if (data?.Type == "routeInfo")
                {
                    MessageBox.Show($"ETA: {data.Eta}\nDistance: {data.Distance}", "Route Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing message: {ex.Message}");
            }
        }

        public class RouteInfoMessage
        {
            public string Type { get; set; } = "";
            public string Eta { get; set; } = "";
            public string Distance { get; set; } = "";
        }

    }
}
