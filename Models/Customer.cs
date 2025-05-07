using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Meditrans.Client.Models
{
    public static class Gender
    {
        public static string Male = "Male";
        public static string Female = "Female";
    }
    public class Customer
    {
        public int Id { get; set; }
        [Required]
        public string FullName { get; set; } = string.Empty;
        [Required]
        public string Address { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string State { get; set; }
        [Required]
        public string Zip { get; set; }
        public string? Phone { get; set; } = string.Empty;
        public string? MobilePhone { get; set; }
        public string? ClientCode { get; set; } = string.Empty;
        public string? PolicyNumber { get; set; }
        [Required]
        public int FundingSourceId { get; set; }
        public FundingSource FundingSource { get; set; }
        [Required]
        public int SpaceTypeId { get; set; }
        public SpaceType SpaceType { get; set; }
        public string? Email { get; set; }
        public DateTime? DOB { get; set; }
        [Required]
        public string Gender { get; set; }
        [Required]
        public DateTime Created { get; set; }
        [Required]
        public string CreatedBy { get; set; }
        public ICollection<Trip> Trips { get; set; }

        /*public override string ToString()
        {
            // This is used to display the default text if DisplayMemberPath is not specified
            return $"{FullName} ({ClientCode}) - {MobilePhone}";
        }*/

    }
}
