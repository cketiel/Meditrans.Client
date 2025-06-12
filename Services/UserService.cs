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
    public class UserService
    {
        private readonly HttpClient _httpClient;
        private readonly string EndPoint = "users";
        public UserService()
        {
            _httpClient = ApiClientFactory.Create();
        }
        public async Task<List<User>> GetAllAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<User>>(EndPoint);
            return result ?? new List<User>();
        }
    }
}
