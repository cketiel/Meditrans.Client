using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Meditrans.Client.Exceptions;
using Meditrans.Client.Models;

namespace Meditrans.Client.Services
{
    public class TripService
    {
        private readonly HttpClient _httpClient;
        private readonly string EndPoint = "trips";
        public TripService()
        {
            _httpClient = ApiClientFactory.Create();
            //_httpClient = new HttpClient();
            //_httpClient.BaseAddress = new Uri("https://localhost:7095/");
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

        public async Task<Trip> CreateTripAsync(Trip trip)
        {          
            try
            {

                var response = await _httpClient.PostAsJsonAsync(EndPoint, trip);

                if (!response.IsSuccessStatusCode)
                {
                    throw await CreateApiException(response, "Error al crear trip");
                }

                return await response.Content.ReadFromJsonAsync<Trip>();
            }
            catch (HttpRequestException ex)
            {
                throw new ApiException("Error de conexión con el servidor", ex);
            }
            
        }

        private async Task<ApiException> CreateApiException(HttpResponseMessage response, string context)
        {
            try
            {
                var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
                return new ApiException(
                    message: $"{context}: {problemDetails?.Title}",
                    statusCode: response.StatusCode,
                    details: problemDetails?.Detail);
            }
            catch
            {
                var content = await response.Content.ReadAsStringAsync();
                return new ApiException(
                    message: $"{context}: Error no especificado",
                    statusCode: response.StatusCode,
                    details: content);
            }
        }
    }
}
