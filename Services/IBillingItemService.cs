using Meditrans.Client.DTOs;
using Meditrans.Client.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Meditrans.Client.Services
{
    public interface IBillingItemService
    {
        Task<List<BillingItemGetDto>> GetBillingItemsAsync();
        Task<BillingItem> CreateBillingItemAsync(BillingItem billingItem);
        Task<BillingItem> UpdateBillingItemAsync(BillingItem billingItem);
        Task DeleteBillingItemAsync(int id);
    }
}