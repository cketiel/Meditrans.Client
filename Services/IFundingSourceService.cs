using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meditrans.Client.Models;

namespace Meditrans.Client.Services
{
    public interface IFundingSourceService
    {
        Task<List<FundingSource>> GetFundingSourcesAsync();
    }
}
