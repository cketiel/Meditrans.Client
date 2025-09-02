using Meditrans.Client.DTOs;
using Meditrans.Client.Models;

namespace Meditrans.Client.Services
{
    public interface IFundingSourceService
    {
        Task<List<FundingSource>> GetFundingSourcesAsync(bool includeInactive);
        Task<FundingSource> CreateFundingSourceAsync(FundingSourceDto dto);
        Task<FundingSource> UpdateFundingSourceAsync(int id, FundingSourceDto dto);
        Task DeleteFundingSourceAsync(int id);
        Task ExportToExcelAsync(List<FundingSource> fundingSources);
    }
}
