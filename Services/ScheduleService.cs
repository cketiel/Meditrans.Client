using System;
using System.Collections.Generic;
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
    public class ScheduleService
    {
        //private readonly string _baseUrl = "https://localhost:7123/api/"; 
        private readonly HttpClient _httpClient;
        private readonly string _endPoint = "schedules";
        private readonly TripHistoryService _historyService;

        public ScheduleService()
        {
            _httpClient = ApiClientFactory.Create();
            _historyService = new TripHistoryService();
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

        public async Task RouteTripsAsync(RouteTripRequest request)
        {
            //var request = new RouteTripRequest { VehicleRouteId = vehicleRouteId, TripIds = tripIds };
            var response = await _httpClient.PostAsJsonAsync($"{_endPoint}/route", request);
            response.EnsureSuccessStatusCode();

            // --- HISTORY RECORD ---
            await _historyService.SaveHistoryAsync(request.TripId, "Run", "Unassigned", request.VehicleRouteName);
        }

        public async Task CancelRouteAsync(int scheduleId)
        {
            var scheduleInfo = await _httpClient.GetFromJsonAsync<ScheduleDto>($"{_endPoint}/{scheduleId}");

            if (scheduleInfo != null && scheduleInfo.TripId.HasValue)
            {             
                var request = new CancelRouteRequest { ScheduleId = scheduleId };
                var response = await _httpClient.PostAsJsonAsync($"{_endPoint}/cancel-route", request);
                response.EnsureSuccessStatusCode();

                await _historyService.SaveHistoryAsync(scheduleInfo.TripId.Value, "Run", scheduleInfo.Run, "Unassigned (Route Cancelled)");
               
            }

            /*var request = new CancelRouteRequest { ScheduleId = scheduleId };
            var response = await _httpClient.PostAsJsonAsync($"{_endPoint}/cancel-route", request);
            response.EnsureSuccessStatusCode();

            await _historyService.SaveHistoryAsync(tripId, "Run", "Assigned", "Unassigned (Route Cancelled)");*/
        }

        public async Task UpdateAsync(int id, ScheduleDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_endPoint}/{id}", dto);

                // We do not expect content, just a success code (204 No Content)
                if (!response.IsSuccessStatusCode)
                {
                    throw await CreateApiException(response, "Error updating schedule");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new ApiException("Connection error with the server.", ex);
            }
        }

        public async Task<List<ProductionReportRowDto>> GetProductionReportDataAsync(DateTime date, int? fundingSourceId)
        {
            // Format the date correctly for the URL query string.
            string formattedDate = date.ToString("yyyy-MM-dd");
            var requestUri = $"{_endPoint}/reports/production?date={formattedDate}";

            // --- NEW: Append the fundingSourceId if it has a value ---
            if (fundingSourceId.HasValue)
            {
                requestUri += $"&fundingSourceId={fundingSourceId.Value}";
            }

            var response = await _httpClient.GetAsync(requestUri);

            response.EnsureSuccessStatusCode(); // Throws an exception if the HTTP response is not successful.

            // The ReadFromJsonAsync method handles JSON deserialization for you.
            return await response.Content.ReadFromJsonAsync<List<ProductionReportRowDto>>();
        }
        private async Task<ApiException> CreateApiException(HttpResponseMessage response, string context)
        {
            try
            {
                var problemDetails = await response.Content.ReadFromJsonAsync<DTOs.ProblemDetails>();

                // Construct a clearer error message, using the issue title if available
                var errorMessage = $"{context}: {problemDetails?.Title ?? "Error not specified by the API."}";

                return new ApiException(
                    message: errorMessage,
                    statusCode: response.StatusCode,
                    details: problemDetails?.Detail);
            }
            catch // If the response body is not valid ProblemDetails JSON
            {
                var content = await response.Content.ReadAsStringAsync();
                return new ApiException(
                    message: $"{context}. Unexpected server response.",
                    statusCode: response.StatusCode,
                    details: content);
            }
        }

    }
}
