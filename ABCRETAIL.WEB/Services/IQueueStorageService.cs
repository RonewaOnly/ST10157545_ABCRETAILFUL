using ABCRETAIL.WEB.Models;

namespace ABCRETAIL.WEB.Services
{
    /// <summary>
    /// Abstraction over Azure Queue Storage, used to carry order transaction
    /// and inventory-processing messages between ABC Retail's ordering
    /// front-end and its (future) processing workers.
    /// </summary>
    public interface IQueueStorageService
    {
        Task InitializeAsync();
        /// <summary>Serializes the order as JSON and enqueues it.</summary>
        Task SendOrderMessageAsync(OrderMessage order);
        /// <summary>
        /// Reads the messages currently sitting in the queue WITHOUT removing
        /// them (a "peek"), so they can be displayed / audited.
        /// </summary>
        Task<IEnumerable<OrderMessage>> PeekMessagesAsync(int maxMessages = 32);
        /// <summary>
        /// Receives and deletes the next message from the queue, simulating
        /// an inventory-processing worker picking up and completing an order.
        /// Returns null if the queue is empty.
        /// </summary>
        Task<OrderMessage?> ProcessNextMessageAsync();
        /// <summary>Approximate number of messages currently in the queue.</summary>
        Task<int> GetApproximateMessageCountAsync();
    }
}
