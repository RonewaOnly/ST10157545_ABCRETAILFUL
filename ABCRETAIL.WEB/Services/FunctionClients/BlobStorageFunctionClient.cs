using System.Net.Http.Json;

namespace ABCRETAIL.WEB.Services.FunctionClients
{
    public class BlobStorageFunctionClient : IBlobStorageService
    {

        private readonly HttpClient _http;

        public BlobStorageFunctionClient(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient("BlobStorageFunction");
        }

        //The function creates its container the first time it runs
        public Task InitializeAsync() => Task.CompletedTask;
        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            var payload = new
            {
                fileName,
                contentType,
                contentBase64 = Convert.ToBase64String(memoryStream.ToArray())
            };
            var response = await _http.PostAsJsonAsync("blob", payload, FunctionJsonOptions.Default);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<BlobUploadResult>(FunctionJsonOptions.Default);
            return result?.Url ?? string.Empty;
        }
        public async Task DeleteFileAsync(string blobNameOrUrl)
        {
            var name = blobNameOrUrl.Contains('/') ? blobNameOrUrl.Split('/').Last() : blobNameOrUrl;
            var response = await _http.DeleteAsync($"blob/{Uri.EscapeDataString(name)}");
            response.EnsureSuccessStatusCode();
        }
        private class BlobUploadResult
        {
            public string Url { get; set; } = string.Empty;
            public string BlobName { get; set; } = string.Empty;
        }
    }
}
