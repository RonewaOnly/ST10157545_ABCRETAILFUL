using Microsoft.AspNetCore.Mvc;
using ABCRETAIL.WEB.Models;
using ABCRETAIL.WEB.Services;

namespace ABCRETAIL.WEB.Controllers
{
    /// <summary>
    /// Manages customer profiles stored in Azure Table Storage.
    /// </summary>
    public class CustomersController : Controller
    {
        private readonly ITableStorageService _tableStorageService;
        public CustomersController(ITableStorageService tableStorageService)
        {
            _tableStorageService = tableStorageService;
        }
        // GET: /Customers
        public async Task<IActionResult> Index()
        {
            var customers = await _tableStorageService.GetAllCustomersAsync();
            return View(customers);
        }
        // GET: /Customers/Details/{rowKey}
        public async Task<IActionResult> Details(string id)
        {
            var customer = await _tableStorageService.GetCustomerAsync(id);
            if (customer == null) return NotFound();
            return View(customer);
        }
        // GET: /Customers/Create
        public IActionResult Create()
        {
            return View(new CustomerProfile());
        }
        // POST: /Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerProfile customer)
        {
            if (!ModelState.IsValid) return View(customer);
            await _tableStorageService.AddCustomerAsync(customer);
            TempData["Success"] = "Customer profile created successfully.";
            return RedirectToAction(nameof(Index));
        }
        // GET: /Customers/Edit/{rowKey}
        public async Task<IActionResult> Edit(string id)
        {
            var customer = await _tableStorageService.GetCustomerAsync(id);
            if (customer == null) return NotFound();
            return View(customer);
        }
        // POST: /Customers/Edit/{rowKey}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, CustomerProfile customer)
        {
            if (!ModelState.IsValid) return View(customer);
            // Re-fetch to obtain the current ETag required for a safe,
            // concurrency-checked update.
            var existing = await _tableStorageService.GetCustomerAsync(id);
            if (existing == null) return NotFound();
            customer.RowKey = existing.RowKey;
            customer.PartitionKey = existing.PartitionKey;
            customer.ETag = existing.ETag;
            await _tableStorageService.UpdateCustomerAsync(customer);
            TempData["Success"] = "Customer profile updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        // GET: /Customers/Delete/{rowKey}
        public async Task<IActionResult> Delete(string id)
        {
            var customer = await _tableStorageService.GetCustomerAsync(id);
            if (customer == null) return NotFound();
            return View(customer);
        }
        // POST: /Customers/Delete/{rowKey}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _tableStorageService.DeleteCustomerAsync("Customer", id);
            TempData["Success"] = "Customer profile deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
