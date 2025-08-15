using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meditrans.Client.DTOs;
using Meditrans.Client.Models;

namespace Meditrans.Client.Services
{
    public interface IUserService
    {
        Task<List<User>> GetUsersAsync();
        Task<User> GetUserByIdAsync(int id); 
        Task<User> AddUserAsync(UserCreateDto userDto);
        Task<bool> UpdateUserAsync(UserUpdateDto userDto);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> ChangePasswordAsync(ChangePasswordDto dto);
    }
}
