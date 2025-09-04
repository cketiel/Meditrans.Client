﻿
using Meditrans.Client.DTOs;
using Meditrans.Client.Models;

namespace Meditrans.Client.Services
{
    public interface IFundingSourceBillingItemService
    {
        Task<List<FundingSourceBillingItemGetDto>> GetAllAsync();
        Task<List<FundingSourceBillingItemGetDto>> GetByFundingSourceIdAsync(int fundingSourceId, bool includeExpired);

        Task<FundingSourceBillingItem> CreateAsync(FundingSourceBillingItemDto dto);
        Task UpdateAsync(int id, FundingSourceBillingItemDto dto);
        Task DeleteAsync(int id);

        Task ExportToExcelAsync(List<FundingSourceBillingItem> items);
    }
}
