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
    public class RunService
    {
        private readonly HttpClient _httpClient;
        private readonly string EndPoint = "runs";
        public RunService()
        {
            _httpClient = ApiClientFactory.Create();
        }
        public async Task<List<VehicleRoute>> GetAllAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<VehicleRoute>>(EndPoint);
            return result ?? new List<VehicleRoute>();
        }

        public async Task<VehicleRoute> CreateAsync(VehicleRoute run)
        {
            try
            {

                var response = await _httpClient.PostAsJsonAsync(EndPoint, run);

                if (!response.IsSuccessStatusCode)
                {
                    throw await CreateApiException(response, "Error creating Run");
                }

                return await response.Content.ReadFromJsonAsync<VehicleRoute>();
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
