using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using Meditrans.Client.Models;

namespace Meditrans.Client.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private string URI = App.Configuration["ApiAddress:ApiTest"];
        //private string URI = App.Configuration["ApiAddress:ApiService"];
        //private string URI = App.Configuration["ApiAddress:UsersService"];
        public AuthService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new System.Uri(URI); 
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
