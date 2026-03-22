using Project.Models;

namespace Project.Services.Database
{
    public interface ICosmosDbService
    {
        Task<FileMetadata> AddFileMetadataAsync(FileMetadata metadata);
        Task<FileMetadata> GetFileMetadataAsync(string id);
        Task<IEnumerable<FileMetadata>> GetAllFileMetadataAsync();
        Task UpdateFileMetadataAsync(string id, FileMetadata metadata);
        Task DeleteFileMetadataAsync(string id);
    }
}