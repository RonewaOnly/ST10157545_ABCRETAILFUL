using Azure.Data.Tables;

namespace ABCRETAIL.WEB.Models
{
    /// <summary>
    /// Represents an order transaction message placed on Azure Queue Storage.
    /// This is NOT a table entity - it is serialized to JSON and sent as the
    /// body of a queue message, which is how ABC Retail's order processing
    /// and inventory-update pipeline communicates asynchronously.
    ///new_word
    /// Message format (JSON):
    /// {
    ///   "OrderId": "guid",
    ///   "CustomerId": "guid",
    ///   "CustomerName": "string",
    ///   "ProductId": "guid",
    ///   "ProductName": "string",
    ///   "Quantity": int,
    ///   "TotalPrice": decimal,
    ///   "OrderDate": "ISO-8601 date",
    ///   "Status": "Queued" | "Processed"
    /// }
    /// </summary>
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
