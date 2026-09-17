namespace ABCRETAIL.WEB.Services
{
    /// <summary>
    /// Abstraction over Azure Blob Storage operations used to host product
    /// images and other multimedia content.
    /// </summary>
    public interface IBlobStorageService
    {
        Task InitializeAsync();

        /// <summary>
        /// Uploads a file stream to Blob Storage and returns the public URL
        /// of the uploaded blob.
        /// </summary>
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);

        /// <summary>Deletes a blob given its full URL or blob name.</summary>
        Task DeleteFileAsync(string blobNameOrUrl);
    }
}
