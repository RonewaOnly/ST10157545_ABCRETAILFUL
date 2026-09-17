using Azure;
using Azure.Data.Tables;
namespace ABCRETAIL.WEB.Models
{
    /// <summary>
    /// Represents product information stored in Azure Table Storage.
    ///
    /// Table Storage design:
    ///   PartitionKey = Category   (groups related products together for
    ///                              efficient partition-scoped queries, e.g.
    ///                              "Electronics", "Clothing")
    ///   RowKey       = ProductId  (unique identifier, generated as a GUID)
    ///
    /// The actual product image / multimedia file itself is NOT stored in the
    /// table (Table Storage is not designed for large binary data). Instead
    /// the file is uploaded to Azure Blob Storage and this entity stores only
    /// the resulting blob URL, keeping the two services cleanly decoupled.
    /// </summary>
    public class Product : ITableEntity
    {
        // ITableEntity required members
        public string PartitionKey { get; set; } = "General";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        // Domain properties
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public int StockQuantity { get; set; }
        /// <summary>URL of the product image stored in Azure Blob Storage.</summary>
        public string? ImageUrl { get; set; }
        /// <summary>Convenience accessors for the views.</summary>
        public string ProductId => RowKey;
        public string Category
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }
    }
}
