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
using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace Meditrans.Client.Views
{
    /// <summary>
    /// Lógica de interacción para HomeView.xaml
    /// </summary>
    public partial class HomeView : UserControl
    {
        public HomeViewModel ViewModel { get; set; }
        private bool _isUpdatingFromHtml = false;
        public HomeView()
        {
            InitializeComponent();
            ViewModel = new HomeViewModel();
            DataContext = ViewModel;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            InitializeData();

            //SetupAutocompleteOverlay();

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
                                //TripInfoLabel.Content = $"ETA: {eta} | Distance: {distance}";
                            });
                            // Here you can display the message
                            //MessageBox.Show($"ETA: {eta}\nDistance: {distance}", "Trip Info");
                        }
                        else if(data.type == "autocomplete")
                        {

                            var result = data.result;
                            //var result = JsonSerializer.Deserialize<MapAddressResult>(data.result ?? "");

                            if (result is null) return;

                            _isUpdatingFromHtml = true;

                            GooglePlacesInput.Text = result.address;
                            //Address.Text = result.address;
                            City.Text = result.city;
                            State.Text = result.state;
                            Zip.Text = result.zip;

                            _isUpdatingFromHtml = false;

                            ShowLocationOnMap((double)result.lat, (double)result.lng);
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

            if (MapaWebView.CoreWebView2 == null)
                return;

            string apiKey = App.Configuration["GoogleMaps:ApiKey"];

            if (trip == null || MapaWebView.CoreWebView2 == null)
            {
                string htmlPath = File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "basemap.html"));
                htmlPath = htmlPath.Replace("{{API_KEY}}", apiKey);

                htmlPath = htmlPath.Replace("{ORIGIN_LAT}", "25.77427")
                                   .Replace("{ORIGIN_LNG}", "-80.19366");
                                  

                MapaWebView.NavigateToString(htmlPath);
            }
            else
            {
                //MessageBox.Show($"La API Key es: {apiKey}");

                string htmlPath = File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "googlemap.html"));
                htmlPath = htmlPath.Replace("{{API_KEY}}", apiKey);

                htmlPath = htmlPath.Replace("{LAT_ORIGEN}", trip.PickupLatitude.ToString())
                                   .Replace("{LNG_ORIGEN}", trip.PickupLongitude.ToString())
                                   .Replace("{LAT_DESTINO}", trip.DropoffLatitude.ToString())
                                   .Replace("{ORIGEN}", trip.PickupAddress)
                                   //.Replace("{NOMBRE}", trip.PatientName)
                                   .Replace("{LNG_DESTINO}", trip.DropoffLongitude.ToString());

                MapaWebView.NavigateToString(htmlPath);

                ShowETAInfo(trip);

            }



                
        }

        private async void GeoLocateButton_Click(object sender, RoutedEventArgs e)
        {
            /*var street = StreetTextBox.Text;
            var city = CityTextBox.Text;
            var state = StateTextBox.Text;
            var zip = ZipTextBox.Text;

            var fullAddress = $"{street}, {city}, {state}, {zip}";

            var coordinates = await GetCoordinatesFromAddress(fullAddress);

            if (coordinates != null)
            {
                // Llama una función JS en el WebView para ubicar en el mapa
                string script = $"setMarkerAt({coordinates.Latitude}, {coordinates.Longitude});";
                await MapaWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
            else
            {
                MessageBox.Show("No se pudo obtener la ubicación", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }*/
        }

        public class Coordinates
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }

        private async Task<Coordinates?> GetCoordinatesFromAddress(string address)
        {
            
            string apiKey = App.Configuration["GoogleMaps:ApiKey"];
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={apiKey}";

            using var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(url);
            var json = JObject.Parse(response);

            if (json["status"]?.ToString() == "OK")
            {
                var location = json["results"]?[0]?["geometry"]?["location"];
                if (location != null)
                {
                    return new Coordinates
                    {
                        Latitude = (double)location["lat"],
                        Longitude = (double)location["lng"]
                    };
                }
            }

            return null;
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
                //ETAInfoLabel.Content = etaLabel.Content;
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



        // Probando
        private async void SetupAutocompleteOverlay()
        {

            string apiKey = App.Configuration["GoogleMaps:ApiKey"];
            

            string path = File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "autocomplete.html"));
            //string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "html", "autocomplete.html");

            path = path.Replace("{{API_KEY}}", apiKey);

            /*await AutocompleteOverlay.EnsureCoreWebView2Async();
            AutocompleteOverlay.NavigateToString(path);
            //AutocompleteOverlay.Source = new Uri(path); // AutocompleteOverlay.NavigateToString(htmlPath);
            AutocompleteOverlay.WebMessageReceived += AutocompleteOverlay_WebMessageReceived;*/
        }

        private void AutocompleteOverlay_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var json = e.TryGetWebMessageAsString();
            var result = JsonSerializer.Deserialize<MapAddressResult>(json ?? "");

            if (result is null) return;

            _isUpdatingFromHtml = true;

            GooglePlacesInput.Text = result.address;
            //Address.Text = result.address;
            City.Text = result.city;
            State.Text = result.state;
            Zip.Text = result.zip;

            _isUpdatingFromHtml = false;

            ShowLocationOnMap(result.lat, result.lng);
        }


        private async void ShowLocationOnMap(double lat, double lng)
        {
            string js = $"setMarkerAt({lat}, {lng});";
            await MapaWebView.ExecuteScriptAsync(js);
        }

        private class MapAddressResult
        {
            public string address { get; set; }
            public string city { get; set; }
            public string state { get; set; }
            public string zip { get; set; }
            public double lat { get; set; }
            public double lng { get; set; }
        }

        private void GooglePlacesInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingFromHtml) return;

            var text = GooglePlacesInput.Text;
            string js = $"document.getElementById('autocompleteInput').value = `{EscapeJs(text)}`;";
            MapaWebView.ExecuteScriptAsync(js);
            //AutocompleteOverlay.ExecuteScriptAsync(js);
        }

        // Escapar texto para uso seguro en JavaScript
        private string EscapeJs(string input)
        {
            return input.Replace("\\", "\\\\").Replace("`", "\\`").Replace("\n", "").Replace("\r", "");
        }

        private void OnSaveCustomerClick(object sender, RoutedEventArgs e)
        {
            // Customer save
            // implementar


            BillingColumn.Width = new GridLength(1, GridUnitType.Star); // 25%
            MapaWebView.SetValue(Grid.ColumnProperty, 3); // El mapa se mueve a la columna 4 (índice 3)
            BillingPanel.Visibility = Visibility.Visible;

            // Hide travel filter
            TripFilterPanel.Visibility = Visibility.Collapsed;

            // Show TabControl
            TripTabs.Visibility = Visibility.Visible;
            //TripTabs.SetValue(Grid.RowProperty, 1);

            // Adjust row size
            TopRow.Height = new GridLength(7, GridUnitType.Star);
            BottomRow.Height = new GridLength(3, GridUnitType.Star);

        }
    }
}
