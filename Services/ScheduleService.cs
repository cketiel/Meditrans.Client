using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Meditrans.Client.DTOs;
using Meditrans.Client.Models;

namespace Meditrans.Client.Services
{
    public class ScheduleService
    {
        //private readonly string _baseUrl = "https://localhost:7123/api/"; // Cambia a tu URL
        private readonly HttpClient _httpClient;
        private readonly string _endPoint = "schedules";

        public ScheduleService()
        {
            _httpClient = ApiClientFactory.Create();
        }

        public async Task<List<ScheduleDto>> GetSchedulesAsync(int routeId, DateTime date)
        {
            //var url = $"{_baseUrl}schedules/by-route?vehicleRouteId={routeId}&date={date:yyyy-MM-dd}";
            var url = $"{_endPoint}/by-route?vehicleRouteId={routeId}&date={date:yyyy-MM-dd}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<ScheduleDto>>();
        }

        public async Task<List<UnscheduledTripDto>> GetUnscheduledTripsAsync(DateTime date)
        {
            var url = $"{_endPoint}/unscheduled?date={date:yyyy-MM-dd}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<UnscheduledTripDto>>();
        }      

        public async Task RouteTripsAsync(int vehicleRouteId, List<int> tripIds)
        {
            var request = new RouteTripRequest { VehicleRouteId = vehicleRouteId, TripIds = tripIds };
            var response = await _httpClient.PostAsJsonAsync($"{_endPoint}/route", request);
            response.EnsureSuccessStatusCode();
        }

        public async Task CancelRouteAsync(int scheduleId)
        {
            var request = new CancelRouteRequest { ScheduleId = scheduleId };
            var response = await _httpClient.PostAsJsonAsync($"{_endPoint}/cancel-route", request);
            response.EnsureSuccessStatusCode();
        }
    }
}
