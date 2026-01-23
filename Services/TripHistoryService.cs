using Meditrans.Client.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Meditrans.Client.Services
{
    public class TripHistoryService
    {
        private readonly HttpClient _httpClient;
        private readonly string EndPoint = "TripHistory"; 

        public TripHistoryService()
        {
            _httpClient = ApiClientFactory.Create();
        }

        public async Task SaveHistoryAsync(int tripId, string field, string priorValue, string newValue)
        {
            var history = new TripHistoryCreateDto
            {
                TripId = tripId,
                ChangeDate = DateTime.Now,
                User = Environment.UserName, // O el nombre del usuario logueado
                Field = field,
                PriorValue = priorValue ?? "N/A",
                NewValue = newValue ?? "N/A"
            };

            await _httpClient.PostAsJsonAsync(EndPoint, history);
        }

        public async Task<List<TripHistoryCreateDto>> GetHistoryByTripAsync(int tripId)
        {
            var result = await _httpClient.GetFromJsonAsync<List<TripHistoryCreateDto>>($"{EndPoint}/{tripId}");
            return result ?? new List<TripHistoryCreateDto>();
        }
    }
}
