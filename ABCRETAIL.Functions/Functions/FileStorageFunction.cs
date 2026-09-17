using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ABCRETAIL.Functions.Models;

namespace ABCRETAIL.Functions.Functions;

public class FileStorageFunction
{
    private readonly ShareDirectoryClient _rootDirectory;
    private readonly ILogger _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public FileStorageFunction(ShareServiceClient shareServiceClient, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var shareName = configuration["AzureStorage:FileShareName"] ?? "abcretail-documents";
        var shareClient = shareServiceClient.GetShareClient(shareName);
        shareClient.CreateIfNotExists();
        _rootDirectory = shareClient.GetRootDirectoryClient();
        _logger = loggerFactory.CreateLogger<FileStorageFunction>();
    }
    public class FileUploadRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentBase64 { get; set; } = string.Empty;
    }
    [Function("FileStorageFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", "delete", Route = "file/{fileName?}")] HttpRequestData req,
        string? fileName)
    {
        switch (req.Method.ToUpperInvariant())
        {
            case "GET" when string.IsNullOrEmpty(fileName):
                return await ListFiles(req);
            case "GET":
                return await DownloadFile(req, fileName!);
            case "POST":
                return await UploadFile(req);
            case "DELETE":
                if (string.IsNullOrEmpty(fileName))
                    return await WriteJson(req, HttpStatusCode.BadRequest, new { error = "fileName is required." });
                await _rootDirectory.GetFileClient(fileName).DeleteIfExistsAsync();
                _logger.LogInformation("Deleted file {FileName} from Azure Files", fileName);
                return req.CreateResponse(HttpStatusCode.NoContent);
            default:
                return req.CreateResponse(HttpStatusCode.MethodNotAllowed);
        }
    }
    private async Task<HttpResponseData> ListFiles(HttpRequestData req)
    {
        var results = new List<StoredFileInfo>();
        await foreach (var item in _rootDirectory.GetFilesAndDirectoriesAsync())
        {
            if (item.IsDirectory) continue;
            var fileClient = _rootDirectory.GetFileClient(item.Name);
            ShareFileProperties props = await fileClient.GetPropertiesAsync();
            results.Add(new StoredFileInfo
            {
                FileName = item.Name,
                SizeBytes = props.ContentLength,
                LastModified = props.LastModified
            });
        }
        return await WriteJson(req, HttpStatusCode.OK, results.OrderByDescending(f => f.LastModified));
    }
    private async Task<HttpResponseData> DownloadFile(HttpRequestData req, string fileName)
    {
        var fileClient = _rootDirectory.GetFileClient(fileName);
        if (!await fileClient.ExistsAsync())
            return req.CreateResponse(HttpStatusCode.NotFound);
        var download = await fileClient.DownloadAsync();
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", download.Value.Details.ContentRange ?? "application/octet-stream");
        response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
        await download.Value.Content.CopyToAsync(response.Body);
        return response;
    }
    private async Task<HttpResponseData> UploadFile(HttpRequestData req)
    {
        using var reader = new StreamReader(req.Body);
        var body = await reader.ReadToEndAsync();
        var upload = JsonSerializer.Deserialize<FileUploadRequest>(body, JsonOptions);
        if (upload == null || string.IsNullOrWhiteSpace(upload.FileName) || string.IsNullOrWhiteSpace(upload.ContentBase64))
            return await WriteJson(req, HttpStatusCode.BadRequest, new { error = "fileName and contentBase64 are required." });
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(upload.ContentBase64);
        }
        catch (FormatException)
        {
            return await WriteJson(req, HttpStatusCode.BadRequest, new { error = "contentBase64 is not valid base64." });
        }
        var fileClient = _rootDirectory.GetFileClient(upload.FileName);
        // Azure Files requires the file to be (re)created at its final
        // size before content can be uploaded into it.
        await fileClient.DeleteIfExistsAsync();
        await fileClient.CreateAsync(bytes.Length);
        if (bytes.Length > 0)
        {
            using var stream = new MemoryStream(bytes);
            await fileClient.UploadRangeAsync(new Azure.HttpRange(0, bytes.Length), stream);
        }
        _logger.LogInformation("Uploaded file {FileName} ({Size} bytes) to Azure Files", upload.FileName, bytes.Length);
        return await WriteJson(req, HttpStatusCode.Created, new { fileName = upload.FileName, sizeBytes = bytes.Length });
    }
    private static async Task<HttpResponseData> WriteJson<T>(HttpRequestData req, HttpStatusCode status, T payload)
    {
        var response = req.CreateResponse(status);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(JsonSerializer.Serialize(payload, JsonOptions));
        return response;
    }
}
