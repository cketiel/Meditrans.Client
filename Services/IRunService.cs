using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meditrans.Client.DTOs;
using Meditrans.Client.Models;

namespace Meditrans.Client.Services
{
    public interface IRunService
    {
        Task<List<VehicleRoute>> GetAllAsync();
        Task<VehicleRoute> GetByIdAsync(int id);
        Task<VehicleRoute> CreateAsync(VehicleRouteDto dto);
        Task UpdateAsync(int id, VehicleRouteDto dto);
        Task DeleteAsync(int id);
    }
}
