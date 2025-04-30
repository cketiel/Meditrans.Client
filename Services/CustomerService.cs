using Meditrans.Client.Models;

using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System;
using System.Windows;

namespace Meditrans.Client.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly HttpClient _httpClient;
        private readonly string EndPoint = "customers";
        //private string URI = App.Configuration["ApiAddress:TripsService"];

        public CustomerService()
        {
            _httpClient = ApiClientFactory.Create();
            //_httpClient = new HttpClient();
            //_httpClient.BaseAddress = new Uri(URI);
        }

        public async Task<List<Customer>> LoadAllCustomersAsync() {

            await Task.Delay(500); // Simula latencia
            var customerList = new List<Customer>();
            try
            {
                var response = await _httpClient.GetAsync(EndPoint);
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




        public async Task<List<Customer>> GetAllAsync()
        {
            var customerList = new List<Customer>();
            try
            {
                var response = await _httpClient.GetAsync(EndPoint);
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
            return await _httpClient.GetFromJsonAsync<Customer>($"{EndPoint}/{id}");
        }

        public async Task<(bool Success, string Message)> CreateCustomerAsync(Customer customer)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(EndPoint, customer);
                //MessageBox.Show(response.ToString());
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Cliente creado correctamente.");
                }

                // Si el servidor responde con error, intenta extraer mensaje
                string errorMessage = await response.Content.ReadAsStringAsync();
                return (false, $"Error del servidor: {errorMessage}");
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Error de conexión: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Error inesperado: {ex.Message}");
            }
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            var response = await _httpClient.PutAsJsonAsync($"{EndPoint}/{customer.Id}", customer);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteCustomerAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{EndPoint}/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
