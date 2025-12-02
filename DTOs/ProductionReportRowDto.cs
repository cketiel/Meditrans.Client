using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meditrans.Client.DTOs
{
    public class ProductionReportRowDto
    {
        public DateTime Date { get; set; }
        public string? Authorization { get; set; }
        public TimeSpan? ReqPickup { get; set; }
        public TimeSpan? Appointment { get; set; }
        public string? Patient { get; set; }
        public string? PickupCity { get; set; }
        public string? Run { get; set; }
        public string? Space { get; set; }
        public TimeSpan? PickupArrive { get; set; }
        public TimeSpan? DropoffArrive { get; set; }
    }
}
