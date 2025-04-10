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

            string htmlPath = File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "map.html"));
            htmlPath = htmlPath.Replace("{LAT_ORIGEN}", trip.PickupLatitude.ToString())
                               .Replace("{LNG_ORIGEN}", trip.PickupLongitude.ToString())
                               .Replace("{LAT_DESTINO}", trip.DropoffLatitude.ToString())
                               .Replace("{ORIGEN}", trip.PickupAddress)
                               .Replace("{NOMBRE}", trip.PatientName)
                               .Replace("{LNG_DESTINO}", trip.DropoffLongitude.ToString());

            MapaWebView.NavigateToString(htmlPath);
          
        }
    }
}
