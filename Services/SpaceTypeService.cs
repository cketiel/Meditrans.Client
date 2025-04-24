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
        private readonly string EndPoint = "spacetypes";
        public SpaceTypeService()
        {
            _httpClient = ApiClientFactory.Create();
        }

        public async Task<List<SpaceType>> GetSpaceTypesAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<SpaceType>>(EndPoint);
            return result ?? new List<SpaceType>();
        }
    }
}
