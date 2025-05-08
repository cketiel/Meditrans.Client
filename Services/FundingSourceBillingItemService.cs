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
    public class FundingSourceBillingItemService: IFundingSourceBillingItemService
    {
        private readonly HttpClient _httpClient;
        private readonly string EndPoint = "FundingSourceBillingItem";
        public FundingSourceBillingItemService()
        {
            _httpClient = ApiClientFactory.Create();
        }

        public async Task<List<FundingSourceBillingItem>> GetAllAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<FundingSourceBillingItem>>(EndPoint);
            return result ?? new List<FundingSourceBillingItem>();
        }
    }
}
