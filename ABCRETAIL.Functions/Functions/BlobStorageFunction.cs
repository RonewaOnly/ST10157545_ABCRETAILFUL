using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ABCRETAIL.Functions.Functions;

public class BlobStorageFunction
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public BlobStorageFunction(BlobServiceClient blobServiceClient, IConfiguration configuration,ILoggerFactory loggerFactory)
    {
        var containerName = configuration["AzureStorage:BlobContainerName"] ?? "product-images";
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        _containerClient.CreateIfNotExists(PublicAccessType.Blob);
        _logger = loggerFactory.CreateLogger<BlobStorageFunction>();
    }

    public class BlobUploadRequest
    {
        public string ? FileName { get; set; }
        public string ContentType { get; set; } = "application/octet-stream";
        public string ? ContentBase64 { get; set; }
    }

    [Function("BlobStorageFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function,"post", "delete", Route = "blob/{blobName?}")] HttpRequestData req, string? blobName
        ) 
    {
        if (req.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();
            var upload = JsonSerializer.Deserialize<BlobUploadRequest>(body, JsonOptions);

            if (upload == null || string.IsNullOrWhiteSpace(upload.FileName) || string.IsNullOrWhiteSpace(upload.ContentBase64))
                return await WriteJson(req, HttpStatusCode.BadRequest, new { error = "FileName and contentBase64 are required" });


            byte[] bytes;

            try
            {
                bytes = Convert.FromBase64String(upload.ContentBase64);

            }
            catch (FormatException)
            {
                return await WriteJson(req, HttpStatusCode.BadRequest, new { error = "contentBase64 is not valid base64 content" });
            }

            //Prefix with a GUID to avoid collisions between uploads that
            //Happen to share the same original file name.
            var generateName = $"{Guid.NewGuid()}_{upload.FileName}";
            var blobClient = _containerClient.GetBlobClient(generateName);

            using var stream = new MemoryStream(bytes);
            await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = upload.ContentType }
            });

            _logger.LogInformation("Uploaded Blob {BlobName} with size {Size} bytes", generateName, bytes.Length);

            return await WriteJson(req, HttpStatusCode.Created, new { url = blobClient.Uri.ToString(), blobName = generateName });
        }

        if(req.Method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
        {
            var target = blobName;
            if(string.IsNullOrWhiteSpace(target))
            {
                var query = ParseQuery(req.Url.Query);
                query.TryGetValue("name", out target);
            }

            if (string.IsNullOrWhiteSpace(target))
                return await WriteJson(req, HttpStatusCode.BadRequest, new {error = "Provide the blob name/URL in the route or via ?name = query string."});

            //Accept either a bare blob name or a full URL
            var name = target.Contains('/') ? target.Split('/').Last() : target;
            await _containerClient.GetBlobClient(name).DeleteIfExistsAsync();

            _logger.LogInformation("Deleted Blob {BlobName}", name);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        return req.CreateResponse(HttpStatusCode.MethodNotAllowed);
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        if(string.IsNullOrWhiteSpace(query))
            return result;

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)) 
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;

            result[key] = value;

        }
        return result;
    }
    
    private static async Task<HttpResponseData> WriteJson<T>(HttpRequestData req, HttpStatusCode status, T payload)
    {
        var response = req.CreateResponse(status);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(JsonSerializer.Serialize(payload, JsonOptions));
        return response;
    }
        /*[Function(nameof(BlobStorageFunction))]
        public async Task Run([BlobTrigger("samples-workitems/{name}", Connection = "connectionString")] Stream stream, string name)
        {
            using var blobStreamReader = new StreamReader(stream);
            var content = await blobStreamReader.ReadToEndAsync();
            _logger.LogInformation("C# Blob trigger function Processed blob\n Name: {name} \n Data: {content}", name, content);
        }*/
    }