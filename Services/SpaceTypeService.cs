using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Meditrans.Client.Models;

namespace Meditrans.Client.Services
{
    public class SpaceTypeService : ISpaceTypeService
    {
        private readonly HttpClient _httpClient;
        private string URI = App.Configuration["ApiAddress:TripsService"];
        public SpaceTypeService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(URI);
        }

        public async Task<List<SpaceType>> GetSpaceTypesAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<SpaceType>>("/api/spacetypes");
            return result ?? new List<SpaceType>();
        }
    }
}
