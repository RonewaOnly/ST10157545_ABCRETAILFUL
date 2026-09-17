using Azure;
using Azure.Data.Tables;
namespace ABCRETAIL.WEB.Models
{
    /// <summary>
    /// Represents a customer profile stored in Azure Table Storage.
    ///
    /// Table Storage design:
    ///   PartitionKey = "Customer"  (single logical partition; could be
    ///                               sharded further, e.g. by region, if the
    ///                               table grows very large)
    ///   RowKey       = CustomerId  (unique identifier, generated as a GUID)
    /// </summary>
    public class CustomerProfile:ITableEntity
    {
        // ITableEntity required members
        public string PartitionKey { get; set; } = "Customer";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Domain properties
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public DateTimeOffset DateRegistered { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>Convenience accessor so views can bind to "Id" instead of RowKey.</summary>
        public string CustomerId => RowKey;
    }
}
