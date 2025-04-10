using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Meditrans.Client.Models;
using Meditrans.Client.Services;

namespace Meditrans.Client.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
    {
        //public ObservableCollection<Trip> Trips { get; set; }
        private ObservableCollection<Trip> _trips;
        public ObservableCollection<Trip> Trips
        {
            get => _trips;
            set
            {
                _trips = value;
                OnPropertyChanged(nameof(Trips)); // Notifica al DataGrid que la lista ha cambiado
            }
        }

        private Trip _selectedTrip;
        public Trip SelectedTrip
        {
            get => _selectedTrip;
            set
            {
                _selectedTrip = value;
                OnPropertyChanged();
            }
        }
        public HomeViewModel() {
            //GetTrips();
            /*var trips = new ObservableCollection<Trip>
            {
                new Trip { SpaceType = "AMB", PatientName="Alicia Ambrose", Date="2025-04-04", FromTime="09:10 AM", PickupAddress="1401 16th Street, Sarasota, FL 34236, EE. UU.", DropoffAddress="240b South Tuttle Avenue, Sarasota, FL 34237, EE. UU.", PickupLatitude=27.351814, PickupLongitude=-82.542549, DropoffLatitude=27.334042, DropoffLongitude=-82.514795 },
                new Trip { SpaceType = "AMB", PatientName="Alicia Ambrose", Date="2025-04-04", FromTime="01:00 PM", PickupAddress="240b South Tuttle Avenue, Sarasota, FL 34237, EE. UU.", DropoffAddress="1401 16th Street, Sarasota, FL 34236, EE. UU.", PickupLatitude=27.334042, PickupLongitude=-82.514795, DropoffLatitude=27.351814, DropoffLongitude=-82.542549 },

                new Trip { SpaceType = "AMB", PatientName="Regina Baker", Date="2025-04-04", FromTime="08:50 AM", PickupAddress="7059 Jarvis Road, Sarasota, FL 34241, EE. UU.", DropoffAddress="2540 South Tamiami Trail, Sarasota, FL 34239, EE. UU.", PickupLatitude=27.292011, PickupLongitude=-82.429845, DropoffLatitude=27.310791, DropoffLongitude=-82.530251 },
                new Trip { SpaceType = "AMB", PatientName="Regina Baker", Date="2025-04-04", FromTime="11:31 AM", PickupAddress="2540 South Tamiami Trail, Sarasota, FL 34239, EE. UU.", DropoffAddress="7059 Jarvis Road, Sarasota, FL 34241, EE. UU.", PickupLatitude=27.310791, PickupLongitude=-82.530251, DropoffLatitude=27.292011, DropoffLongitude=-82.429845 }

            };
            Trips = trips;
            SelectedTrip = Trips[0];*/
        }

        public async void GetTrips()
        {
            var tripService = new TripService();
            Trips = await tripService.GetTripsAsync(); 
            SelectedTrip = Trips[0];
        }

        public async void LoadTripsFromApi()
        {
            var tripList = new List<Trip>();
            var tripService = new TripService();
            tripList = await tripService.GetTripList();
            if (tripList != null && tripList.Any())
            {
                Trips = new ObservableCollection<Trip>(tripList);
                SelectedTrip = Trips.First(); // 
            }
            else
            {
                // Show message or log: no data arrived
            }  
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
