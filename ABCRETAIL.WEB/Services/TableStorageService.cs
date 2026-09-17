using Azure;
using Azure.Data.Tables;
using ABCRETAIL.WEB.Models;

namespace ABCRETAIL.WEB.Services
{
    /// <summary>
    /// Concrete implementation of ITableStorageService backed by Azure Table
    /// Storage. Two tables are used: one for customer profiles and one for
    /// product information, as required by the assignment brief.
    /// </summary>
    public class TableStorageService : ITableStorageService
    {
        private readonly TableClient _customerTable;
        private readonly TableClient _productTable;
        public TableStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("AzureStorage:ConnectionString is not configured.");
            var customerTableName = configuration["AzureStorage:CustomerTableName"] ?? "CustomerProfiles";
            var productTableName = configuration["AzureStorage:ProductTableName"] ?? "Products";
            var serviceClient = new TableServiceClient(connectionString);
            _customerTable = serviceClient.GetTableClient(customerTableName);
            _productTable = serviceClient.GetTableClient(productTableName);
        }
        public async Task InitializeAsync()
        {
            // CreateIfNotExists is idempotent - safe to call every startup.
            await _customerTable.CreateIfNotExistsAsync();
            await _productTable.CreateIfNotExistsAsync();
        }
        // ---------------- Customers ----------------
        public async Task<IEnumerable<CustomerProfile>> GetAllCustomersAsync()
        {
            var results = new List<CustomerProfile>();
            await foreach (var entity in _customerTable.QueryAsync<CustomerProfile>())
            {
                results.Add(entity);
            }
            return results.OrderBy(c => c.FullName);
        }
        public async Task<CustomerProfile?> GetCustomerAsync(string rowKey)
        {
            try
            {
                var response = await _customerTable.GetEntityAsync<CustomerProfile>("Customer", rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }
        public async Task AddCustomerAsync(CustomerProfile customer)
        {
            await _customerTable.AddEntityAsync(customer);
        }
        public async Task UpdateCustomerAsync(CustomerProfile customer)
        {
            // Merge mode preserves any properties not included in this update,
            // Replace mode (used here) fully overwrites the entity, which is
            // what we want for a standard "edit profile" form submission.
            await _customerTable.UpdateEntityAsync(customer, customer.ETag, TableUpdateMode.Replace);
        }
        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            await _customerTable.DeleteEntityAsync(partitionKey, rowKey);
        }
        // ---------------- Products ----------------
        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            var results = new List<Product>();
            await foreach (var entity in _productTable.QueryAsync<Product>())
            {
                results.Add(entity);
            }
            return results.OrderBy(p => p.Category).ThenBy(p => p.ProductName);
        }
        public async Task<Product?> GetProductAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _productTable.GetEntityAsync<Product>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }
        public async Task AddProductAsync(Product product)
        {
            await _productTable.AddEntityAsync(product);
        }
        public async Task UpdateProductAsync(Product product)
        {
            await _productTable.UpdateEntityAsync(product, product.ETag, TableUpdateMode.Replace);
        }
        public async Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            await _productTable.DeleteEntityAsync(partitionKey, rowKey);
        }
    }
}
