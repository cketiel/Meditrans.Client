using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meditrans.Client.Models;

namespace Meditrans.Client.Services
{
    public interface ISpaceTypeService
    {
        Task<List<SpaceType>> GetSpaceTypesAsync();
    }
}
