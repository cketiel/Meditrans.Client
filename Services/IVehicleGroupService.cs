using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meditrans.Client.Models;

namespace Meditrans.Client.Services
{
    public interface IVehicleGroupService
    {
        Task<List<VehicleGroup>> GetGroupsAsync();
        Task<VehicleGroup> CreateGroupAsync(VehicleGroup vehicleGroup);
        Task<bool> DeleteGroupAsync(int id);
    }
}
