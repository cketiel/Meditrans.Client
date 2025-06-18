using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Meditrans.Client.DTOs;
using Meditrans.Client.Exceptions;
using Meditrans.Client.Models;

namespace Meditrans.Client.Services
{
    // Implement the interface to promote decoupling
    public class RunService : IRunService
    {
        private readonly HttpClient _httpClient;
        private readonly string _endPoint = "runs"; 

        public RunService()
        {           
            _httpClient = ApiClientFactory.Create();
        }

        public async Task<List<VehicleRoute>> GetAllAsync()
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<List<VehicleRoute>>(_endPoint);
                return result ?? new List<VehicleRoute>();
            }
            catch (HttpRequestException ex)
            {
                // This error occurs if the server is unavailable
                throw new ApiException("Connection error with the server.", ex);
            }
        }

        public async Task<VehicleRoute> GetByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_endPoint}/{id}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null; 
                }

                response.EnsureSuccessStatusCode(); // Throws exception for other error codes (500, 401, etc.)
                return await response.Content.ReadFromJsonAsync<VehicleRoute>();
            }
            catch (HttpRequestException ex)
            {
                throw new ApiException("Connection error with the server.", ex);
            }
        }

        public async Task<VehicleRoute> CreateAsync(VehicleRouteDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(_endPoint, dto);

                if (!response.IsSuccessStatusCode)
                {                   
                    throw await CreateApiException(response, "Error creating route");
                }

                // The API returns the created entity with its new ID
                return await response.Content.ReadFromJsonAsync<VehicleRoute>();
            }
            catch (HttpRequestException ex)
            {
                throw new ApiException("Connection error with the server.", ex);
            }
        }

        public async Task UpdateAsync(int id, VehicleRouteDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_endPoint}/{id}", dto);

                // We do not expect content, just a success code (204 No Content)
                if (!response.IsSuccessStatusCode)
                {
                    throw await CreateApiException(response, "Error updating route");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new ApiException("Connection error with the server.", ex);
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_endPoint}/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    throw await CreateApiException(response, "Error deleting route");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new ApiException("Connection error with the server.", ex);
            }
        }

        // Helper to create custom and detailed exceptions from HTTP response
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