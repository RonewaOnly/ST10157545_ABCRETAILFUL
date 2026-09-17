using Microsoft.AspNetCore.Mvc;
using ABCRETAIL.WEB.Services;

namespace ABCRETAIL.WEB.Controllers
{
    /// <summary>
    /// Manages documents (e.g. invoices, reports) stored in an Azure Files
    /// share, retrievable by their original file name.
    /// </summary>
    public class DocumentsController : Controller
    {
        private readonly IFileStorageService _fileStorageService;
        public DocumentsController(IFileStorageService fileStorageService)
        {
            _fileStorageService = fileStorageService;
        }
        // GET: /Documents
        public async Task<IActionResult> Index()
        {
            var files = await _fileStorageService.ListFilesAsync();
            return View(files);
        }
        // POST: /Documents/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file is not { Length: > 0 })
            {
                TempData["Error"] = "Please choose a file to upload.";
                return RedirectToAction(nameof(Index));
            }
            using var stream = file.OpenReadStream();
            // File is stored using its own original file name, as required.
            await _fileStorageService.UploadFileAsync(stream, file.FileName);
            TempData["Success"] = $"'{file.FileName}' uploaded to Azure Files.";
            return RedirectToAction(nameof(Index));
        }
        // GET: /Documents/Download/{fileName}
        public async Task<IActionResult> Download(string fileName)
        {
            var result = await _fileStorageService.DownloadFileAsync(fileName);
            if (result == null) return NotFound();
            return File(result.Value.Content, result.Value.ContentType, fileName);
        }
        // POST: /Documents/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string fileName)
        {
            await _fileStorageService.DeleteFileAsync(fileName);
            TempData["Success"] = $"'{fileName}' deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
