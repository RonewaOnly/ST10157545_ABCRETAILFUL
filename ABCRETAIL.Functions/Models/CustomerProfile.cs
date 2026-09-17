using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCRETAIL.Functions.Models
{
    public class CustomerProfile : ITableEntity
    {
        public string PartitionKey { get; set; } = "Customer";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string ? FullName { get; set; }
        public string ? Email { get; set; }
        public string ? PhoneNumber { get; set; }
        public string ? ShippingAddress { get; set; }
        public string ? City { get; set; }
        public string ? PostalCode { get; set; }
        public string ? Country { get; set; }
        public DateTimeOffset DateRegistered { get; set; } = DateTimeOffset.UtcNow;
        public string CustomerId => RowKey;
    }
}
