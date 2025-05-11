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
using System.Globalization;
using Meditrans.Client.Helpers;
using System.Windows.Media.Animation;
using MaterialDesignColors;
using System.Windows.Media.Media3D;
using Meditrans.Client.Exceptions;

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
                // Subscribe to message from JavaScript
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
                                if (DataContext is HomeViewModel vm)
                                {
                                    vm.Distance = distance; // double.Parse(distance); // meters
                                    vm.ETA = eta; // double.Parse(eta); // seconds
                                }
                                //TripInfoLabel.Content = $"ETA: {eta} | Distance: {distance}";
                            });
                            // Here you can display the message
                            //MessageBox.Show($"ETA: {eta}\nDistance: {distance}", "Trip Info");
                        }
                        else if (data.type == "autocomplete")
                        {
                            var result = data.result;

                            if (result is null) return;

                            _isUpdatingFromHtml = true;

                            GooglePlacesInput.Text = result.address;
                            //Address.Text = result.address;
                            City.Text = result.city;
                            State.Text = result.state;
                            Zip.Text = result.zip;

                            _isUpdatingFromHtml = false;

                            //ShowLocationOnMap((double)result.lat, (double)result.lng);
                        }
                        else if (data.type == "dropoff")
                        {
                            if (data.dropoff_address is null) return;

                            _isUpdatingFromHtml = true;
                            DropoffAddressTextBox.Text = data.dropoff_address;
                            _isUpdatingFromHtml = false;
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

        public void LoadMapExcample()
        {
            // Template HTML file path
            string templatePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "googlemap.html");

            // Read original HTML
            string htmlContent = File.ReadAllText(templatePath);

            // Read key from App.config
            string apiKey = ConfigurationManager.AppSettings["GoogleMapsApiKey"];

            // Replace {{API_KEY}} with the real one
            htmlContent = htmlContent.Replace("{{API_KEY}}", apiKey);

            // Crear archivo temporal con el HTML ya modificado
            string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "googlemap2.html");
            File.WriteAllText(tempPath, htmlContent);

            // Load into WebView2

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
            string js = $"document.getElementById('pickup').value = `{EscapeJs(text)}`;";
            MapaWebView.ExecuteScriptAsync(js);
            //AutocompleteOverlay.ExecuteScriptAsync(js);
        }

        // Escapar texto para uso seguro en JavaScript
        private string EscapeJs(string input)
        {
            return input.Replace("\\", "\\\\").Replace("`", "\\`").Replace("\n", "").Replace("\r", "");
        }

        private async void OnSaveCustomerClick(object sender, RoutedEventArgs e)
        {
            // Customer save           
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            //culture = CultureInfo.InvariantCulture;

            var customer = new CustomerCreateDto
            {
                FullName = FullNameTextBox.Text,
                ClientCode = ClientCodeTextBox.Text,
                Phone = PhoneTextBox.Text,
                MobilePhone = MobilePhoneTextBox.Text,

                SpaceTypeId = Convert.ToInt16(SpaceTypeComboBox.SelectedValue), 
                FundingSourceId = Convert.ToInt16(FundingSourceComboBox.SelectedValue),

                Address = GooglePlacesInput.Text,
                City = City.Text,
                State = State.Text,
                Zip = Zip.Text,
                //DOB = DateTime.ParseExact(DOBDatePicker.Text, "MM/dd/yyyy", culture),
                Gender = MaleRadioButton.IsChecked == true? MaleRadioButton.Content.ToString(): FemaleRadioButton.Content.ToString(),

                Created = DateTime.Now,
                CreatedBy = SessionManager.Username

            };

            if (DOBDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Por favor selecciona una fecha de nacimiento válida.");
                return;
            }
            customer.DOB = DOBDatePicker.SelectedDate.Value;

            // ✅ Intentamos convertir la fecha con TryParse
            if (DateTime.TryParse(DOBDatePicker.Text, out var dob))
            {
                customer.DOB = dob;
            }
            else
            {
                MessageBox.Show("Fecha de nacimiento inválida. Usa un formato válido.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            /*bool customerExists = false;
            var (updateSuccess, updateMessage) = (false,"");
            var (success, message, response) = (false, "", new object());*/

            bool success = false;

            CustomerService _customerService = new CustomerService();
            if (DataContext is HomeViewModel vm)
            {
                Customer selectedCustomer = vm.SelectedCustomer;
                if (selectedCustomer?.Id == null)
                {
                    try
                    {
                        var createdCustomer = await _customerService.CreateCustomerAsync(customer);
                        success = true;
                        vm.IdCustomer = createdCustomer.Id;
                    }
                    catch (ApiException ex)
                    {
                        MessageBox.Show(
                            $"Error {ex.StatusCode}:\n{ex.ErrorDetails}",
                            "Error del servidor",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error inesperado: {ex.Message}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                 
                }
                else
                {
                    try
                    {
                        //customer.Id = selectedCustomer.Id; MessageBox.Show(customer.ToString());
                        var updatedCustomer = await _customerService.UpdateCustomerAsync(selectedCustomer.Id, customer);
                        success = true;
                        vm.IdCustomer = selectedCustomer.Id;
                    }
                    catch (ApiException ex)
                    {
                        MessageBox.Show(
                            $"Error {ex.StatusCode}:\n{ex.ErrorDetails}",
                            "Error del servidor",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error inesperado: {ex.Message}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                  
                }                   
            }

                       

            if (success)
            {
                //MessageBox.Show(message); // luego crear servicios de mensajes, avisos y alertas

                CustomerColumn.Width = new GridLength(3.2, GridUnitType.Star);
                BillingColumn.Width = new GridLength(2.4, GridUnitType.Star); // 20%
                MapColumn.Width = new GridLength(4.4, GridUnitType.Star);
                MapaWebView.SetValue(Grid.ColumnProperty, 4); // El mapa se mueve a la columna 5 (índice 4)
                BillingPanel.Visibility = Visibility.Visible;
                ForDateCalendar.Visibility = Visibility.Visible;


                // Hide travel filter
                TripFilterPanel.Visibility = Visibility.Collapsed;

                // Show TabControl
                TripTabs.Visibility = Visibility.Visible;
                //TripTabs.SetValue(Grid.RowProperty, 1);
               // PickupAddressTextBox.Text = GooglePlacesInput.Text;

                // Adjust row size
                TopRow.Height = new GridLength(6.73, GridUnitType.Star);
                BottomRow.Height = new GridLength(3.27, GridUnitType.Star);

                // Show Dropoff input in WebView map
                await MapaWebView.ExecuteScriptAsync("showDropoff();");
            }
            else
            {
                // MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }




        }

        private async void OnNewCustomerClick(object sender, RoutedEventArgs e) {
            await MapaWebView.ExecuteScriptAsync("prepareNewCustomer();");

        }

        private void CustomersAutoSuggestBox_SuggestionChosen(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Patch to autocomplete bug.
            if (DataContext is HomeViewModel vm)
            {              
                Customer customer = vm.Customers.FirstOrDefault(c => c.Id == int.Parse(e.NewValue.ToString()));

                // MessageBox.Show(a?.FullName + " a.FullName");
                vm.SelectedCustomer = customer;
                vm.SearchText = customer?.FullName;

                ShowPickupInMap();
            }
        }

        private void CustomersAutoSuggestBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void AppointmentRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            PickupTimePicker.Visibility = Visibility.Visible;
            ApptTimePicker.Visibility = Visibility.Visible;
            ReturnTimePicker.Visibility = Visibility.Collapsed;
            WillCallCheckBox.Visibility = Visibility.Collapsed;

            string tripType = TripType.Appointment;
            //TripTypeTextBlock.Text = tripType;  
            BillingSectionTabItem.Header = tripType + " " + ForDateCalendar.SelectedDate.Value.Date.ToShortDateString() + " " + "Hora";
            if (DataContext is HomeViewModel vm)
            {
                vm.TripType = tripType;
            }
        }

        private void ReturnRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            PickupTimePicker.Visibility = Visibility.Collapsed;
            ApptTimePicker.Visibility = Visibility.Collapsed;
            ReturnTimePicker.Visibility = Visibility.Visible;
            WillCallCheckBox.Visibility = Visibility.Visible;

            string tripType = TripType.Return;
            TripTypeTextBlock.Text = tripType;
            BillingSectionTabItem.Header = TripType.Return + " " + ForDateCalendar.SelectedDate.Value.Date.ToShortDateString() + " " + "Hora";
        }

        private void GeolocateFloatingButton_MouseEnter(object sender, MouseEventArgs e)
        {
            Color primaryColor;
            SolidColorBrush backgroundBrush;
            if (GeolocateFloatingButton.Background is SolidColorBrush brush)
            {
                primaryColor = brush.Color;
            }

            GeolocateFloatingButton.Background = Brushes.Transparent;

            // Crear animaciones
            var widthAnimation = new DoubleAnimation(35, TimeSpan.FromSeconds(0.3));
            var heightAnimation = new DoubleAnimation(35, TimeSpan.FromSeconds(0.3));
            var colorAnimation = new ColorAnimation(primaryColor, TimeSpan.FromSeconds(0.3));

            // Configurar interpolación suave
            widthAnimation.EasingFunction = new QuadraticEase();
            heightAnimation.EasingFunction = new QuadraticEase();

            // Aplicar animaciones
            GeolocateFloatingButton.BeginAnimation(WidthProperty, widthAnimation);
            GeolocateFloatingButton.BeginAnimation(HeightProperty, heightAnimation);

            // Animación del color de fondo
            backgroundBrush = new SolidColorBrush(Colors.Transparent);
            GeolocateFloatingButton.Background = backgroundBrush;
            backgroundBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        private void GeolocateFloatingButton_MouseLeave(object sender, MouseEventArgs e)
        {
            SolidColorBrush backgroundBrush = new SolidColorBrush(Colors.Transparent);

            if (GeolocateFloatingButton.Background is SolidColorBrush brush)
            {
                backgroundBrush = new SolidColorBrush(brush.Color);
            }
            // Animaciones inversas
            var widthAnimation = new DoubleAnimation(30, TimeSpan.FromSeconds(0.3));
            var heightAnimation = new DoubleAnimation(30, TimeSpan.FromSeconds(0.3));
            var colorAnimation = new ColorAnimation(Colors.Transparent, TimeSpan.FromSeconds(0.3));

            // Configurar interpolación suave
            widthAnimation.EasingFunction = new QuadraticEase();
            heightAnimation.EasingFunction = new QuadraticEase();

            // Aplicar animaciones
            GeolocateFloatingButton.BeginAnimation(WidthProperty, widthAnimation);
            GeolocateFloatingButton.BeginAnimation(HeightProperty, heightAnimation);

            // Animación del color de fondo
            backgroundBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        private void GeolocateFloatingButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPickupInMap();           
        }

        private async void ShowPickupInMap()
        {
            var street = GooglePlacesInput.Text;
            var city = City.Text;
            var state = State.Text;
            var zip = Zip.Text;
            
            var fullAddress = $"{street}, {city}, {state}, {zip}";

            GoogleMapsService googleMapsService = new GoogleMapsService();
            var coordinates = await googleMapsService.GetCoordinatesFromAddress(fullAddress);           
            //var coordinates = await GetCoordinatesFromAddress(fullAddress);

            if (coordinates != null)
            {
                //position = { lat, lng }
                // Format using CultureInfo.InvariantCulture to avoid issues with commas/points
                var positionJson = $"{{ lat: {coordinates.Latitude.ToString(CultureInfo.InvariantCulture)}, lng: {coordinates.Longitude.ToString(CultureInfo.InvariantCulture)} }}";
                string script = $@"
                    if (typeof setPickupMarker === 'function') {{
                        setPickupMarker({positionJson});
                    }} else {{
                        console.error('setPickupMarker function is not defined');
                    }}
                ";
                //string script = $"setPickupMarker({positionJson});"; 
                // Call a JS function in the WebView to locate on the map
                //string script = $"setPickupMarker({coordinates.Latitude}, {coordinates.Longitude});"; 

                await MapaWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
            else
            {
                MessageBox.Show("Could not get location", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RoundTripRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            AppointmentRadioButton.Visibility = Visibility.Collapsed;
            ReturnRadioButton.Visibility = Visibility.Collapsed;

            PickupTimePicker.Visibility = Visibility.Visible;
            ApptTimePicker.Visibility = Visibility.Visible;
            ReturnTimePicker.Visibility = Visibility.Visible;
            WillCallCheckBox.Visibility = Visibility.Visible;
        }

        private void OneWayRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            AppointmentRadioButton.Visibility = Visibility.Visible;
            ReturnRadioButton.Visibility = Visibility.Visible;
        }

        private void DropoffAddressTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingFromHtml) return;

            var text = DropoffAddressTextBox.Text;
            string js = $"document.getElementById('dropoff').value = `{EscapeJs(text)}`;";
            MapaWebView.ExecuteScriptAsync(js);
            //AutocompleteOverlay.ExecuteScriptAsync(js);
        }

        // State variable to control the icon
        private bool isAutorenewOn = false;
        private double currentRotation = 0;
        private void InverterFloatingButton_Click(object sender, RoutedEventArgs e)
        {       
            // Toggle status
            isAutorenewOn = !isAutorenewOn;

            // Change icon based on status
            InverterIcon.Kind = isAutorenewOn ?
                MaterialDesignThemes.Wpf.PackIconKind.Autorenew :
                MaterialDesignThemes.Wpf.PackIconKind.AutorenewOff;

            //invertir los valores y luego llamar a ShowPickupInMap() y ShowDropoffInMap()
        }

        private void SaveTab1FloatingButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
