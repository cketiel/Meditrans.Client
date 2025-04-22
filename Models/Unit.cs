using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meditrans.Client.Models
{
    public class Unit
    {
        public int Id { get; set; }
        public string Abbreviation { get; set; }
        public string Description { get; set; }
        public ICollection<BillingItem> BillingItems { get; set; }
    }
}
