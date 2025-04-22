using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Meditrans.Client.Models;

namespace Meditrans.Client.Services
{
    public class TripService
    {
        private readonly HttpClient _httpClient;

        public TripService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7095/");
        }

        public async Task<ObservableCollection<Trip>> GetTripsAsync()
        {
            var trips = await _httpClient.GetFromJsonAsync<ObservableCollection<Trip>>("api/trips");
            //var trips = await _httpClient.GetFromJsonAsync<List<Trip>>("api/trips");
            //return trips ?? new List<Trip>();
            return trips ?? new ObservableCollection<Trip>();
        }

        public async Task<ObservableCollection<Trip>> GetAllTrips()
        {
            ObservableCollection<Trip> Trips = new ObservableCollection<Trip>();
            Trip SelectedTrip;
            var response = await _httpClient.GetAsync("api/trips");
            if (response.IsSuccessStatusCode)
            {
                var tripList = await response.Content.ReadFromJsonAsync<List<Trip>>();
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
            return Trips;
        }
        public async Task<List<Trip>> GetTripList()
        {
            var tripList = new List<Trip>();
            try
            {
                var response = await _httpClient.GetAsync("api/trips");
                if (response.IsSuccessStatusCode)
                {
                    tripList = await response.Content.ReadFromJsonAsync<List<Trip>>();

                }
            }
            catch (Exception ex) { 
                // throw exception 
            }

            return tripList;
        }
    }
}
