using Microsoft.AspNetCore.Mvc;
using ABCRETAIL.WEB.Models;
using ABCRETAIL.WEB.Services;

namespace ABCRETAIL.WEB.Controllers
{
    /// <summary>
    /// Manages product information (Azure Table Storage) together with the
    /// associated product image / multimedia content (Azure Blob Storage).
    /// </summary>
    public class ProductsController : Controller
    {
        private readonly ITableStorageService _tableStorageService;
        private readonly IBlobStorageService _blobStorageService;

        public ProductsController(ITableStorageService tableStorageService, IBlobStorageService blobStorageService)
        {
            _tableStorageService = tableStorageService;
            _blobStorageService = blobStorageService;
        }

        // GET: /Products
        public async Task<IActionResult> Index()
        {
            var products = await _tableStorageService.GetAllProductsAsync();
            return View(products);
        }

        // GET: /Products/Details/{partitionKey}/{rowKey}
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var product = await _tableStorageService.GetProductAsync(partitionKey, rowKey);
            if (product == null) return NotFound();
            return View(product);
        }

        // GET: /Products/Create
        public IActionResult Create()
        {
            return View(new Product());
        }

        // POST: /Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(product);

            // Product image / multimedia content is uploaded to Blob Storage;
            // only the resulting URL is persisted on the Table Storage entity.
            if (imageFile is { Length: > 0 })
            {
                using var stream = imageFile.OpenReadStream();
                product.ImageUrl = await _blobStorageService.UploadFileAsync(
                    stream, imageFile.FileName, imageFile.ContentType);
            }

            await _tableStorageService.AddProductAsync(product);
            TempData["Success"] = "Product created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Products/Edit/{partitionKey}/{rowKey}
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var product = await _tableStorageService.GetProductAsync(partitionKey, rowKey);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: /Products/Edit/{partitionKey}/{rowKey}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey, Product product, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(product);

            var existing = await _tableStorageService.GetProductAsync(partitionKey, rowKey);
            if (existing == null) return NotFound();

            product.RowKey = existing.RowKey;
            product.PartitionKey = existing.PartitionKey;
            product.ETag = existing.ETag;
            product.ImageUrl = existing.ImageUrl;

            // Replace the image in Blob Storage only if a new file was supplied.
            if (imageFile is { Length: > 0 })
            {
                if (!string.IsNullOrEmpty(existing.ImageUrl))
                {
                    await _blobStorageService.DeleteFileAsync(existing.ImageUrl);
                }

                using var stream = imageFile.OpenReadStream();
                product.ImageUrl = await _blobStorageService.UploadFileAsync(
                    stream, imageFile.FileName, imageFile.ContentType);
            }

            await _tableStorageService.UpdateProductAsync(product);
            TempData["Success"] = "Product updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Products/Delete/{partitionKey}/{rowKey}
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var product = await _tableStorageService.GetProductAsync(partitionKey, rowKey);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: /Products/Delete/{partitionKey}/{rowKey}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            var product = await _tableStorageService.GetProductAsync(partitionKey, rowKey);
            if (product != null && !string.IsNullOrEmpty(product.ImageUrl))
            {
                await _blobStorageService.DeleteFileAsync(product.ImageUrl);
            }

            await _tableStorageService.DeleteProductAsync(partitionKey, rowKey);
            TempData["Success"] = "Product deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
