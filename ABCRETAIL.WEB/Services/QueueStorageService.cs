using ABCRETAIL.WEB.Models;
using System.Text.Json;
using Azure.Storage.Queues;


namespace ABCRETAIL.WEB.Services
{
    /// <summary>
    /// Concrete implementation of IQueueStorageService backed by Azure Queue
    /// Storage. Order transactions are serialized to JSON so both the web
    /// app and any downstream processing service (e.g. an Azure Function)
    /// can read a consistent, well-defined message format.
    /// </summary>
    public class QueueStorageService : IQueueStorageService
    {
        private readonly QueueClient _queueClient;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

        public QueueStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("AzureStorage:ConnectionString is not configured.");

            var queueName = configuration["AzureStorage:OrderQueueName"] ?? "order-processing";

            // Base64-encode message bodies so any character set (including
            // stray control characters) round-trips safely.
            _queueClient = new QueueClient(connectionString, queueName,
                new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
        }

        public async Task InitializeAsync()
        {
            await _queueClient.CreateIfNotExistsAsync();
        }

        public async Task SendOrderMessageAsync(OrderMessage order)
        {
            var json = JsonSerializer.Serialize(order, JsonOptions);
            await _queueClient.SendMessageAsync(json);
        }

        public async Task<IEnumerable<OrderMessage>> PeekMessagesAsync(int maxMessages = 32)
        {
            // Azure Queue Storage allows peeking up to 32 messages at a time.
            var capped = Math.Min(maxMessages, 32);
            var response = await _queueClient.PeekMessagesAsync(capped);

            var results = new List<OrderMessage>();
            foreach (var msg in response.Value)
            {
                var order = TryDeserialize(msg.MessageText);
                if (order != null) results.Add(order);
            }
            return results;
        }

        public async Task<OrderMessage?> ProcessNextMessageAsync()
        {
            var response = await _queueClient.ReceiveMessageAsync();
            var message = response.Value;
            if (message == null) return null;

            var order = TryDeserialize(message.MessageText);
            if (order != null) order.Status = "Processed";

            // Remove the message from the queue now that it has been "processed".
            await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);

            return order;
        }

        public async Task<int> GetApproximateMessageCountAsync()
        {
            var props = await _queueClient.GetPropertiesAsync();
            return props.Value.ApproximateMessagesCount;
        }

        private static OrderMessage? TryDeserialize(string messageText)
        {
            try
            {
                return JsonSerializer.Deserialize<OrderMessage>(messageText);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
