using Project.Models;

namespace Project.Services.Storage
{
    public interface IAzureBlobStorageService
    {
        Task<List<BlobObject>> GetBlobsAsync();
        Task<BlobObject> UploadAsync(IFormFile formFile);
        Task DeleteBlobAsync(string blobName);
    }
}