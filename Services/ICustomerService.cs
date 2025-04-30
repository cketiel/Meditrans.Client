using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meditrans.Client.Models;

namespace Meditrans.Client.Services
{
    public interface ICustomerService
    {
        //Task<List<Customer>> SearchCustomersAsync(string query);
        Task<List<Customer>> LoadAllCustomersAsync();
        Task<List<Customer>> GetAllAsync();
        Task<Customer> GetCustomerByIdAsync(int id);
        Task<(bool Success, string Message)> CreateCustomerAsync(Customer customer);
        //Task<Customer> CreateCustomerAsync(Customer customer);
        Task<bool> UpdateCustomerAsync(Customer customer);
        Task<bool> DeleteCustomerAsync(int id);
    }
}
