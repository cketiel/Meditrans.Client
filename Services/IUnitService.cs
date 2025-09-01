using Meditrans.Client.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Meditrans.Client.Services
{
    public interface IUnitService
    {
        Task<List<Unit>> GetUnitsAsync();
    }
}
