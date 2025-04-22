using Meditrans.Client.Models;

using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System;

namespace Meditrans.Client.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly HttpClient _httpClient;
        private string URI = App.Configuration["ApiAddress:TripsService"];

        public CustomerService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(URI);
        }

        /*public async Task<List<Customer>> SearchCustomersAsync(string query)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<Customer>>($"/api/customers?search={query}");
                return response ?? new List<Customer>();
            }
            catch (Exception)
            {
                return new List<Customer>();
            }
        }*/

        public async Task<List<Customer>> GetAllAsync()
        {
            var customerList = new List<Customer>();
            try
            {
                var response = await _httpClient.GetAsync("api/customers");
                if (response.IsSuccessStatusCode)
                {
                    customerList = await response.Content.ReadFromJsonAsync<List<Customer>>();

                }
            }
            catch (Exception ex)
            {
                // throw exception 
            }

            return customerList;
        }

        public async Task<Customer> GetCustomerByIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<Customer>($"/api/customers/{id}");
        }

        public async Task<Customer> CreateCustomerAsync(Customer customer)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/customers", customer);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Customer>();
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/customers/{customer.Id}", customer);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"/api/customers/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
