namespace ABCRETAIL.WEB.Services
{
    public class StoredFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTimeOffset? LastModified { get; set; }
    }
    /// <summary>
    /// Abstraction over Azure Files (an SMB/REST-accessible file share),
    /// used to store documents such as invoices and reports, retrievable by
    /// their original file name.
    /// </summary>
    public interface IFileStorageService
    {
        Task InitializeAsync();
        Task UploadFileAsync(Stream fileStream, string fileName);
        Task<IEnumerable<StoredFileInfo>> ListFilesAsync();
        /// <summary>Downloads a file's content by its file name. Returns null if not found.</summary>
        Task<(Stream Content, string ContentType)?> DownloadFileAsync(string fileName);
        Task DeleteFileAsync(string fileName);
    }
}
