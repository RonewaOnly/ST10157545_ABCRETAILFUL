using Azure.Storage.Queues;
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

public class QueueStorageFunction
{
    private readonly QueueClient _queueClient;

    private readonly ILogger _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public QueueStorageFunction(QueueServiceClient queueServiceClient, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var queueName = configuration["AzureStorage:OrderQueueName"] ?? "orders-processing";
        _queueClient = queueServiceClient.GetQueueClient(queueName);
        _queueClient.CreateIfNotExists();

        _logger = loggerFactory.CreateLogger<QueueStorageFunction>();
    }

    [Function("QueueStorageFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function,"get","post", Route = "queues/{action?}")] HttpRequestData req, string? action
        )
    {
        switch (action?.ToLowerInvariant())
        {
            case null or "":
                if (!req.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                    return req.CreateResponse(HttpStatusCode.MethodNotAllowed);
                return await SendOrder(req);

            case "peek":
                return await PeekMessages(req);

            case "process":
                return await ProcessNext(req);
            case "count":
                return await GetCount(req);

            default:
                return await WriteJson(req, HttpStatusCode.BadRequest, new {error = $"Unknown action '{action}'" });
        }
        
    }

    private async Task<HttpResponseData> SendOrder(HttpRequestData req)
    {
        using var reader = new StreamReader(req.Body);
        var body = await reader.ReadToEndAsync();
        var order = JsonSerializer.Deserialize<OrderMessage>(body, JsonOptions);
        if (order == null)
            return await WriteJson(req, HttpStatusCode.BadRequest, new { error = "Invalid order payload." });
        var json = JsonSerializer.Serialize(order, JsonOptions);
        await _queueClient.SendMessageAsync(json);
        _logger.LogInformation("Enqueued order {OrderId} for product {ProductName}", order.OrderId, order.ProductName);
        return await WriteJson(req, HttpStatusCode.Created, order);
    }
    private async Task<HttpResponseData> PeekMessages(HttpRequestData req)
    {
        var maxMessages = 32;
        var query = ParseQuery(req.Url.Query);
        if (query.TryGetValue("max", out var maxStr) && int.TryParse(maxStr, out var parsed))
            maxMessages = Math.Clamp(parsed, 1, 32);
        var response = await _queueClient.PeekMessagesAsync(maxMessages);
        var results = new List<OrderMessage>();
        foreach (var msg in response.Value)
        {
            var order = TryDeserialize(msg.MessageText);
            if (order != null) results.Add(order);
        }
        return await WriteJson(req, HttpStatusCode.OK, results);
    }
    private async Task<HttpResponseData> ProcessNext(HttpRequestData req)
    {
        var response = await _queueClient.ReceiveMessageAsync();
        var message = response.Value;
        if (message == null) return req.CreateResponse(HttpStatusCode.NoContent);
        var order = TryDeserialize(message.MessageText);
        if (order != null) order.Status = "Processed";
        await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
        _logger.LogInformation("Processed order {OrderId}", order?.OrderId ?? "(unparseable message, discarded)");
        return await WriteJson(req, HttpStatusCode.OK, order);
    }
    private async Task<HttpResponseData> GetCount(HttpRequestData req)
    {
        var props = await _queueClient.GetPropertiesAsync();
        return await WriteJson(req, HttpStatusCode.OK, new { count = props.Value.ApproximateMessagesCount });
    }
    private static OrderMessage? TryDeserialize(string messageText)
    {
        try
        {
            return JsonSerializer.Deserialize<OrderMessage>(messageText, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query)) return result;
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


    /*[Function(nameof(QueueStorageFunction))]
    public async Task Run([BlobTrigger("samples-workitems/{name}", Connection = "connectionString")] Stream stream, string name)
    {
        using var blobStreamReader = new StreamReader(stream);
        var content = await blobStreamReader.ReadToEndAsync();
        _logger.LogInformation("C# Blob trigger function Processed blob\n Name: {name} \n Data: {content}", name, content);
    }*/

}