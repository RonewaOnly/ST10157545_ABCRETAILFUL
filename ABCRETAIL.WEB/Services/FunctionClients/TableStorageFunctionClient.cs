using ABCRETAIL.WEB.Models;
using System.Net;
using System.Net.Http.Json;


namespace ABCRETAIL.WEB.Services.FunctionClients
{
    public class TableStorageFunctionClient : ITableStorageService
    {
        private readonly HttpClient _http;

        public TableStorageFunctionClient(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient("AzureFunctions");
        }

        //The function creates its table the first time it runs
        //is nothing for the web app to do on startup any moew


        public Task InitializeAsync() => Task.CompletedTask;
        // ---------------- Customers ----------------
        public async Task<IEnumerable<CustomerProfile>> GetAllCustomersAsync()
        {
            var result = await _http.GetFromJsonAsync<List<CustomerProfile>>("table/customers", FunctionJsonOptions.Default);
            return result ?? new List<CustomerProfile>();
        }
        public async Task<CustomerProfile?> GetCustomerAsync(string rowKey)
        {
            var response = await _http.GetAsync($"table/customers/{Uri.EscapeDataString(rowKey)}");
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CustomerProfile>(FunctionJsonOptions.Default);
        }
        public async Task AddCustomerAsync(CustomerProfile customer)
        {
            var response = await _http.PostAsJsonAsync("table/customers", customer, FunctionJsonOptions.Default);
            response.EnsureSuccessStatusCode();
        }
        public async Task UpdateCustomerAsync(CustomerProfile customer)
        {
            var response = await _http.PutAsJsonAsync(
                $"table/customers/{Uri.EscapeDataString(customer.RowKey)}", customer, FunctionJsonOptions.Default);
            response.EnsureSuccessStatusCode();
        }
        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            var response = await _http.DeleteAsync($"table/customers/{Uri.EscapeDataString(rowKey)}");
            response.EnsureSuccessStatusCode();
        }
        // ---------------- Products ----------------
        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            var result = await _http.GetFromJsonAsync<List<Product>>("table/products", FunctionJsonOptions.Default);
            return result ?? new List<Product>();
        }
        public async Task<Product?> GetProductAsync(string partitionKey, string rowKey)
        {
            var response = await _http.GetAsync(
                $"table/products/{Uri.EscapeDataString(partitionKey)}/{Uri.EscapeDataString(rowKey)}");
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Product>(FunctionJsonOptions.Default);
        }
        public async Task AddProductAsync(Product product)
        {
            var response = await _http.PostAsJsonAsync("table/products", product, FunctionJsonOptions.Default);
            response.EnsureSuccessStatusCode();
        }
        public async Task UpdateProductAsync(Product product)
        {
            var response = await _http.PutAsJsonAsync(
                $"table/products/{Uri.EscapeDataString(product.PartitionKey)}/{Uri.EscapeDataString(product.RowKey)}",
                product, FunctionJsonOptions.Default);
            response.EnsureSuccessStatusCode();
        }
        public async Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            var response = await _http.DeleteAsync(
                $"table/products/{Uri.EscapeDataString(partitionKey)}/{Uri.EscapeDataString(rowKey)}");
            response.EnsureSuccessStatusCode();
        }
    }
}
