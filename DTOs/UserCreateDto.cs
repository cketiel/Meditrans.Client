using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meditrans.Client.DTOs
{
    public class UserCreateDto
    {
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; } 
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public int RoleId { get; set; }
    }
}
