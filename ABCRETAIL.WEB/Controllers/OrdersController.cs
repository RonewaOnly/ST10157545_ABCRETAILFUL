using Microsoft.AspNetCore.Mvc;
using ABCRETAIL.WEB.Models;
using ABCRETAIL.WEB.Services;

namespace ABCRETAIL.WEB.Controllers
{
    /// <summary>
    /// Handles order placement and inventory processing using Azure Queue
    /// Storage. Placing an order deducts stock from the corresponding
    /// Product entity in Table Storage and enqueues a transaction message;
    /// "processing" an order (simulating a background worker) dequeues the
    /// next message.
    /// </summary>
    public class OrdersController : Controller
    {
        private readonly IQueueStorageService _queueStorageService;
        private readonly ITableStorageService _tableStorageService;
        public OrdersController(IQueueStorageService queueStorageService, ITableStorageService tableStorageService)
        {
            _queueStorageService = queueStorageService;
            _tableStorageService = tableStorageService;
        }
        // GET: /Orders  - shows queued transactions waiting to be processed
        public async Task<IActionResult> Index()
        {
            ViewBag.QueueCount = await _queueStorageService.GetApproximateMessageCountAsync();
            var messages = await _queueStorageService.PeekMessagesAsync();
            return View(messages);
        }
        // GET: /Orders/Create - order placement form
        public async Task<IActionResult> Create()
        {
            ViewBag.Customers = await _tableStorageService.GetAllCustomersAsync();
            ViewBag.Products = await _tableStorageService.GetAllProductsAsync();
            return View();
        }
        // POST: /Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string customerId, string productPartitionKey, string productRowKey, int quantity)
        {
            var customer = await _tableStorageService.GetCustomerAsync(customerId);
            var product = await _tableStorageService.GetProductAsync(productPartitionKey, productRowKey);
            if (customer == null || product == null || quantity < 1)
            {
                TempData["Error"] = "Please select a valid customer, product and quantity.";
                return RedirectToAction(nameof(Create));
            }
            if (product.StockQuantity < quantity)
            {
                TempData["Error"] = $"Insufficient stock for {product.ProductName}. Only {product.StockQuantity} left.";
                return RedirectToAction(nameof(Create));
            }
            // 1. Deduct inventory in Table Storage.
            product.StockQuantity -= quantity;
            await _tableStorageService.UpdateProductAsync(product);
            // 2. Enqueue the order transaction message onto Azure Queue Storage.
            var order = new OrderMessage
            {
                CustomerId = customer.RowKey,
                CustomerName = customer.FullName,
                ProductId = product.RowKey,
                ProductCategory = product.Category,
                ProductName = product.ProductName,
                Quantity = quantity,
                TotalPrice = product.Price * quantity
            };
            await _queueStorageService.SendOrderMessageAsync(order);
            TempData["Success"] = $"Order for {quantity} x {product.ProductName} placed and queued for processing.";
            return RedirectToAction(nameof(Index));
        }
        // POST: /Orders/ProcessNext - simulates an inventory-processing worker
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessNext()
        {
            var processed = await _queueStorageService.ProcessNextMessageAsync();
            TempData["Success"] = processed != null
                ? $"Processed order {processed.OrderId} for {processed.ProductName}."
                : "No orders were waiting in the queue.";
            return RedirectToAction(nameof(Index));
        }
    }
}
