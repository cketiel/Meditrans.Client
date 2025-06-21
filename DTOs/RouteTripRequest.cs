using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meditrans.Client.DTOs
{
    // For routing request
    public class RouteTripRequest
    {
        [Required]
        public int VehicleRouteId { get; set; }
        [Required]
        public List<int> TripIds { get; set; }
    }
}
