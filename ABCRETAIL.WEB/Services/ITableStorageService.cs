using ABCRETAIL.WEB.Models;

namespace ABCRETAIL.WEB.Services
{
    /// <summary>
    /// Abstraction over Azure Table Storage operations for both
    /// CustomerProfile and Product entities.
    /// </summary>
    public interface ITableStorageService
    {
        Task InitializeAsync();
        // Customers
        Task<IEnumerable<CustomerProfile>> GetAllCustomersAsync();
        Task<CustomerProfile?> GetCustomerAsync(string rowKey);
        Task AddCustomerAsync(CustomerProfile customer);
        Task UpdateCustomerAsync(CustomerProfile customer);
        Task DeleteCustomerAsync(string partitionKey, string rowKey);
        // Products
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductAsync(string partitionKey, string rowKey);
        Task AddProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(string partitionKey, string rowKey);
    }
}
