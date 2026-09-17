using System;
using System.Collections.Generic;
using System.Text;

namespace ABCRETAIL.Functions.Models
{
    public class OrderMessage
    {
        public string OrderId { get; set; } = Guid.NewGuid().ToString();
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductCategory { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public double TotalPrice { get; set; }
        public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;
        public string Status { get; set; } = "Queued";
    }
}
