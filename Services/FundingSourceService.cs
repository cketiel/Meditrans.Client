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
        private readonly string EndPoint = "fundingsources";
        public FundingSourceService()
        {
            _httpClient = ApiClientFactory.Create();
        }

        public async Task<List<FundingSource>> GetFundingSourcesAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<FundingSource>>(EndPoint);
            return result ?? new List<FundingSource>();
        }
    }
}
