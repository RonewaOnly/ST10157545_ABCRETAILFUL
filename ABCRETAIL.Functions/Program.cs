using Azure.Data.Tables;
using Azure.Monitor.OpenTelemetry.Exporter;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Storage.Queues;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}
var connectionString = builder.Configuration["AzureStorage:ConnectionString"] ?? "UseDevelopmentStorage=true";

builder.Services.AddSingleton(
    new TableServiceClient(connectionString));

builder.Services.AddSingleton(
    new BlobServiceClient(connectionString));

builder.Services.AddSingleton(
    new QueueServiceClient(connectionString, new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }));

builder.Services.AddSingleton(
    new ShareServiceClient(connectionString));

builder.Build().Run();
