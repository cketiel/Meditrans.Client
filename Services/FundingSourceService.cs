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
    public class FundingSourceService : IFundingSourceService
    {
        private readonly HttpClient _httpClient;
        private string URI = App.Configuration["ApiAddress:TripsService"];
        public FundingSourceService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(URI);
        }

        public async Task<List<FundingSource>> GetFundingSourcesAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<FundingSource>>("/api/fundingsources");
            return result ?? new List<FundingSource>();
        }
    }
}
