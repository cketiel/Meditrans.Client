using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meditrans.Client.DTOs
{
    // For cancellation request
    public class CancelRouteRequest
    {
        [Required]
        public int ScheduleId { get; set; }
    }
}
