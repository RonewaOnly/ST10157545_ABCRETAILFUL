using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace ABCRETAIL.WEB.Services
{
    /// <summary>
    /// Concrete implementation of IBlobStorageService backed by Azure Blob
    /// Storage. Used to host product images and multimedia content.
    /// </summary>
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;


    public BlobStorageService(IConfiguration configuration)
        {
            var connectionString =
                configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "AzureStorage:ConnectionString is not configured.");

            var containerName =
                configuration["AzureStorage:BlobContainerName"]
                ?? "products";

            var serviceClient =
                new BlobServiceClient(connectionString);

            _containerClient =
                serviceClient.GetBlobContainerClient(containerName);
        }

        /// <summary>
        /// Creates the Blob container if it does not already exist.
        /// The container remains private because public Blob access is
        /// disabled on the Azure Storage Account.
        /// </summary>
        public async Task InitializeAsync()
        {
            await _containerClient.CreateIfNotExistsAsync();
        }

        /// <summary>
        /// Uploads a file to Azure Blob Storage and returns the blob URL.
        /// </summary>
          public async Task<string> UploadFileAsync(
            Stream fileStream,
            string fileName,
            string contentType)
        {
            if (fileStream == null)
            {
                throw new ArgumentNullException(nameof(fileStream));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException(
                    "File name is required.",
                    nameof(fileName));
            }

            var blobName =
                $"{Guid.NewGuid()}-{Path.GetFileName(fileName)}";

            var blobClient =
                _containerClient.GetBlobClient(blobName);

            var headers = new BlobHttpHeaders
            {
                ContentType = string.IsNullOrWhiteSpace(contentType)
                    ? "application/octet-stream"
                    : contentType
            };

            await blobClient.UploadAsync(
                fileStream,
                new BlobUploadOptions
                {
                    HttpHeaders = headers
                });

            // Generate a temporary read-only SAS URL.
            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException(
                    "The BlobClient cannot generate a SAS URI. " +
                    "Make sure the application is using a Storage Account connection string.");
            }

            var sasBuilder = new Azure.Storage.Sas.BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(20000)
            };

            sasBuilder.SetPermissions(
                Azure.Storage.Sas.BlobSasPermissions.Read);

            var sasUri =
                blobClient.GenerateSasUri(sasBuilder);

            return sasUri.ToString();


        }


        /// <summary>
        /// Deletes a blob using either its blob name or its URL.
        /// </summary>
        public async Task DeleteFileAsync(string blobNameOrUrl)
        {
            if (string.IsNullOrWhiteSpace(blobNameOrUrl))
            {
                return;
            }

            string blobName;

            if (Uri.TryCreate(
                    blobNameOrUrl,
                    UriKind.Absolute,
                    out var uri))
            {
                blobName =
                    Uri.UnescapeDataString(
                        uri.AbsolutePath
                            .Split('/', StringSplitOptions.RemoveEmptyEntries)
                            .Last());
            }
            else
            {
                blobName = blobNameOrUrl;
            }

            var blobClient =
                _containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync();
        }
    }

}
