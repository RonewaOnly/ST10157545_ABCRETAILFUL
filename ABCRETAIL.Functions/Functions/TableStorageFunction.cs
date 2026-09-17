using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ABCRETAIL.Functions.Models;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace ABCRETAIL.Functions.Functions;

public class TableStorageFunction
{
    private readonly TableClient _customerTable;
    private readonly TableClient _productTable;
    private readonly ILogger _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public TableStorageFunction(TableServiceClient tableServiceClient, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var customerTableName = configuration["AzureStorage:CustomerTableName"] ?? "CustomerProfiles";
        var productTableName = configuration["AzureStorage:ProductTableName"] ?? "Products";
        _customerTable = tableServiceClient.GetTableClient(customerTableName);
        _productTable = tableServiceClient.GetTableClient(productTableName);
        _customerTable.CreateIfNotExists();
        _productTable.CreateIfNotExists();
        _logger = loggerFactory.CreateLogger<TableStorageFunction>();
    }
    [Function("TableStorageFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", "put", "delete",
        Route = "table/{entity}/{key1?}/{key2?}")] HttpRequestData req,
        string entity, string? key1, string? key2)
    {
        _logger.LogInformation("TableStorageFunction: {Method} entity={Entity}", req.Method, entity);
        try
        {
            return entity.ToLowerInvariant() switch
            {
                "customers" => await HandleCustomers(req, key1),
                "products" => await HandleProducts(req, key1, key2),
                _ => await WriteJson(req, HttpStatusCode.BadRequest,
                    new { error = $"Unknown entity '{entity}'. Use 'customers' or 'products'." })
            };
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Table Storage request failed");
            var status = Enum.IsDefined(typeof(HttpStatusCode), ex.Status) ? (HttpStatusCode)ex.Status : HttpStatusCode.BadRequest;
            return await WriteJson(req, status, new { error = ex.Message });
        }
    }
    private async Task<HttpResponseData> HandleCustomers(HttpRequestData req, string? rowKey)
    {
        switch (req.Method.ToUpperInvariant())
        {
            case "GET" when string.IsNullOrEmpty(rowKey):
                {
                    var results = new List<CustomerProfile>();
                    await foreach (var c in _customerTable.QueryAsync<CustomerProfile>())
                        results.Add(c);
                    return await WriteJson(req, HttpStatusCode.OK, results.OrderBy(c => c.FullName));
                }
            case "GET":
                {
                    try
                    {
                        var response = await _customerTable.GetEntityAsync<CustomerProfile>("Customer", rowKey);
                        return await WriteJson(req, HttpStatusCode.OK, response.Value);
                    }
                    catch (RequestFailedException ex) when (ex.Status == 404)
                    {
                        return req.CreateResponse(HttpStatusCode.NotFound);
                    }
                }
            case "POST":
                {
                    var customer = await ReadJson<CustomerProfile>(req);
                    if (customer == null)
                        return await WriteJson(req, HttpStatusCode.BadRequest, new { error = "Invalid customer payload." });
                    await _customerTable.AddEntityAsync(customer);
                    return await WriteJson(req, HttpStatusCode.Created, customer);
                }
            case "PUT":
                {
                    if (string.IsNullOrEmpty(rowKey))
                        return await WriteJson(req, HttpStatusCode.BadRequest, new { error = "rowKey is required." });
                    var customer = await ReadJson<CustomerProfile>(req);
                    if (customer == null)
                        return await WriteJson(req, HttpStatusCode.BadRequest, new { error = "Invalid customer payload." });
                    customer.RowKey = rowKey;
                    customer.PartitionKey = "Customer";
                    await _customerTable.UpdateEntityAsync(customer, ETag.All, TableUpdateMode.Replace);
                    return await WriteJson(req, HttpStatusCode.OK, customer);
                }
            case "DELETE":
                {
                    if (string.IsNullOrEmpty(rowKey))
                        return await WriteJson(req, HttpStatusCode.BadRequest, new { error = "rowKey is required." });
                    await _customerTable.DeleteEntityAsync("Customer", rowKey);
                    return req.CreateResponse(HttpStatusCode.NoContent);
                }
            default:
                return req.CreateResponse(HttpStatusCode.MethodNotAllowed);
        }
    }
    private async Task<HttpResponseData> HandleProducts(HttpRequestData req, string? partitionKey, string? rowKey)
    {
        switch (req.Method.ToUpperInvariant())
        {
            case "GET" when string.IsNullOrEmpty(partitionKey):
                {
                    var results = new List<Product>();
                    await foreach (var p in _productTable.QueryAsync<Product>())
                        results.Add(p);
                    return await WriteJson(req, HttpStatusCode.OK, results.OrderBy(p => p.Category).ThenBy(p => p.ProductName));
                }
            case "GET":
                {
                    if (string.IsNullOrEmpty(rowKey))
                        return await WriteJson(req, HttpStatusCode.BadRequest,
                            new { error = "Both partitionKey and rowKey are required to fetch a single product." });
                    try
                    {
                        var response = await _productTable.GetEntityAsync<Product>(partitionKey, rowKey);
                        return await WriteJson(req, HttpStatusCode.OK, response.Value);
                    }
                    catch (RequestFailedException ex) when (ex.Status == 404)
                    {
                        return req.CreateResponse(HttpStatusCode.NotFound);
                    }
                }
            case "POST":
                {
                    var product = await ReadJson<Product>(req);
                    if (product == null)
                        return await WriteJson(req, HttpStatusCode.BadRequest, new { error = "Invalid product payload." });
                    await _productTable.AddEntityAsync(product);
                    return await WriteJson(req, HttpStatusCode.Created, product);
                }
            case "PUT":
                {
                    if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                        return await WriteJson(req, HttpStatusCode.BadRequest, new { error = "partitionKey and rowKey are required." });
                    var product = await ReadJson<Product>(req);
                    if (product == null)
                        return await WriteJson(req, HttpStatusCode.BadRequest, new { error = "Invalid product payload." });
                    product.PartitionKey = partitionKey;
                    product.RowKey = rowKey;
                    await _productTable.UpdateEntityAsync(product, ETag.All, TableUpdateMode.Replace);
                    return await WriteJson(req, HttpStatusCode.OK, product);
                }
            case "DELETE":
                {
                    if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                        return await WriteJson(req, HttpStatusCode.BadRequest, new { error = "partitionKey and rowKey are required." });
                    await _productTable.DeleteEntityAsync(partitionKey, rowKey);
                    return req.CreateResponse(HttpStatusCode.NoContent);
                }
            default:
                return req.CreateResponse(HttpStatusCode.MethodNotAllowed);
        }
    }
    private static async Task<T?> ReadJson<T>(HttpRequestData req)
    {
        using var reader = new StreamReader(req.Body);
        var body = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(body)) return default;
        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }
    private static async Task<HttpResponseData> WriteJson<T>(HttpRequestData req, HttpStatusCode status, T payload)
    {
        var response = req.CreateResponse(status);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(JsonSerializer.Serialize(payload, JsonOptions));
        return response;
    }
}