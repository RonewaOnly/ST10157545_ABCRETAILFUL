using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace ABCRETAIL.WEB.Services
{
    /// <summary>
    /// Concrete implementation of IFileStorageService backed by an Azure
    /// Files share. Files are stored flat in the share's root directory
    /// using their original file name, as required by the assignment brief
    /// ("Files have been stored ... using their file names").
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private readonly ShareClient _shareClient;

        public FileStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("AzureStorage:ConnectionString is not configured.");

            var shareName = configuration["AzureStorage:FileShareName"] ?? "abcretail-documents";

            _shareClient = new ShareClient(connectionString, shareName);
        }

        public async Task InitializeAsync()
        {
            await _shareClient.CreateIfNotExistsAsync();
        }

        private ShareDirectoryClient RootDirectory => _shareClient.GetRootDirectoryClient();

        public async Task UploadFileAsync(Stream fileStream, string fileName)
        {
            var fileClient = RootDirectory.GetFileClient(fileName);

            // Azure Files requires the file to be created at its final size
            // up front, then the content uploaded into it.
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            await fileClient.CreateAsync(memoryStream.Length);
            await fileClient.UploadRangeAsync(
                new Azure.HttpRange(0, memoryStream.Length),
                memoryStream);
        }

        public async Task<IEnumerable<StoredFileInfo>> ListFilesAsync()
        {
            var results = new List<StoredFileInfo>();

            await foreach (var item in RootDirectory.GetFilesAndDirectoriesAsync())
            {
                if (item.IsDirectory) continue;

                var fileClient = RootDirectory.GetFileClient(item.Name);
                ShareFileProperties props = await fileClient.GetPropertiesAsync();

                results.Add(new StoredFileInfo
                {
                    FileName = item.Name,
                    SizeBytes = props.ContentLength,
                    LastModified = props.LastModified
                });
            }

            return results.OrderByDescending(f => f.LastModified);
        }

        public async Task<(Stream Content, string ContentType)?> DownloadFileAsync(string fileName)
        {
            var fileClient = RootDirectory.GetFileClient(fileName);

            if (!await fileClient.ExistsAsync()) return null;

            var download = await fileClient.DownloadAsync();
            var contentType = download.Value.Details.ContentDisposition ?? "application/octet-stream";
            return (download.Value.Content, contentType);
        }

        public async Task DeleteFileAsync(string fileName)
        {
            var fileClient = RootDirectory.GetFileClient(fileName);
            await fileClient.DeleteIfExistsAsync();
        }
    }
}
