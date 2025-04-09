using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Meditrans.Client.Models;

namespace Meditrans.Client.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;

        public AuthService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new System.Uri("https://localhost:7151/"); // en un archivo de configuracion
        }

        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            var request = new LoginRequest
            {
                Username = username,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            
            if (response.IsSuccessStatusCode)
            {
                
                return await response.Content.ReadFromJsonAsync<LoginResponse>();
            }

            return new LoginResponse { IsSuccess = false, Message = "Invalid login" };
        }
    }
}
