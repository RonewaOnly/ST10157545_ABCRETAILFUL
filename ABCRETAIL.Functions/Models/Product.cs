using Azure;
using Azure.Data.Tables;

using System;
using System.Collections.Generic;
using System.Text;

namespace ABCRETAIL.Functions.Models
{
    public class Product : ITableEntity
    {
        public string PartitionKey { get; set; } = "General";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }
        public string ProductId => RowKey;
        public string Category
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }
    }
}
