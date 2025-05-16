using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Meditrans.Client.DTOs;
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
        }

        public async Task<List<TripReadDto>> GetAllTripsAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<TripReadDto>>(EndPoint);
            return result ?? new List<TripReadDto>();
        }
        /*public async Task<List<TripReadDto>> GetAllTripsAsync2()
        {  
            try
            {
                var response = await _httpClient.GetAsync(EndPoint);

                if (!response.IsSuccessStatusCode)
                {
                    throw await CreateApiException(response, "Error getting trips");
                }

                return await response.Content.ReadFromJsonAsync<List<TripReadDto>>();
            }
            catch (HttpRequestException ex)
            {
                throw new ApiException("Server connection error", ex);
            }
        }
        public async Task<ObservableCollection<Trip>> GetTripsAsync()
        {
            var trips = await _httpClient.GetFromJsonAsync<ObservableCollection<Trip>>("api/trips");          
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
        }*/

        public async Task<TripReadDto> CreateTripAsync(Trip trip)
        {          
            try
            {

                var response = await _httpClient.PostAsJsonAsync(EndPoint, trip);

                if (!response.IsSuccessStatusCode)
                {                  
                    throw await CreateApiException(response, "Error creating trip");
                }

                return await response.Content.ReadFromJsonAsync<TripReadDto>();
            }
            catch (HttpRequestException ex)
            {
                throw new ApiException("Server connection error", ex);
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
                    message: $"{context}: Unspecified error",
                    statusCode: response.StatusCode,
                    details: content);
            }
        }
    }
}
