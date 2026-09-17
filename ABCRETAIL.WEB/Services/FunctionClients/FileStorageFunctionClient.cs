using System.Net;

namespace ABCRETAIL.WEB.Services.FunctionClients
{
    public class FileStorageFunctionClient : IFileStorageService
    {

        private readonly HttpClient _http;

        public FileStorageFunctionClient(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient("AzureFunctions");
        }

        //The function creates its file share the first time it runs
        public Task InitializeAsync() => Task.CompletedTask;
        public async Task UploadFileAsync(Stream fileStream, string fileName)
        {
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            var payload = new
            {
                fileName,
                contentBase64 = Convert.ToBase64String(memoryStream.ToArray())
            };
            var response = await _http.PostAsJsonAsync("file", payload, FunctionJsonOptions.Default);
            response.EnsureSuccessStatusCode();
        }
        public async Task<IEnumerable<StoredFileInfo>> ListFilesAsync()
        {
            var result = await _http.GetFromJsonAsync<List<StoredFileInfo>>("file", FunctionJsonOptions.Default);
            return result ?? new List<StoredFileInfo>();
        }
        public async Task<(Stream Content, string ContentType)?> DownloadFileAsync(string fileName)
        {
            var response = await _http.GetAsync($"file/{Uri.EscapeDataString(fileName)}");
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var stream = await response.Content.ReadAsStreamAsync();
            return (stream, contentType);
        }
        public async Task DeleteFileAsync(string fileName)
        {
            var response = await _http.DeleteAsync($"file/{Uri.EscapeDataString(fileName)}");
            response.EnsureSuccessStatusCode();
        }
    }
}
