using Microsoft.Azure.Cosmos;
using Project.Models;

namespace Project.Services.Database
{
    public class CosmosDbService : ICosmosDbService
    {
        private readonly Container _container;

        public CosmosDbService(CosmosClient cosmosClient, IConfiguration configuration)
        {
            var databaseName = configuration["CosmosDb:DatabaseName"];
            var containerName = configuration["CosmosDb:ContainerName"];
            _container = cosmosClient.GetContainer(databaseName, containerName);
        }

        public async Task<FileMetadata> AddFileMetadataAsync(FileMetadata metadata)
        {
            metadata.Id = Guid.NewGuid().ToString();
            metadata.UploadDate = DateTime.UtcNow;
            metadata.Category ??= "inne";
            
            var response = await _container.CreateItemAsync(metadata, new PartitionKey(metadata.Category));
            return response.Resource;
        }

        public async Task<FileMetadata> GetFileMetadataAsync(string id)
        {
            var query = _container.GetItemQueryIterator<FileMetadata>(
                new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                    .WithParameter("@id", id));
            
            if (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                return response.FirstOrDefault();
            }
            
            return null;
        }

        public async Task<IEnumerable<FileMetadata>> GetAllFileMetadataAsync()
        {
            var query = _container.GetItemQueryIterator<FileMetadata>(
                new QueryDefinition("SELECT * FROM c ORDER BY c.uploadDate DESC"));
            
            var results = new List<FileMetadata>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response);
            }
            
            return results;
        }

        public async Task UpdateFileMetadataAsync(string id, FileMetadata metadata)
        {
            metadata.Category ??= "inne";
            await _container.UpsertItemAsync(metadata, new PartitionKey(metadata.Category));
        }

        public async Task DeleteFileMetadataAsync(string id)
        {
            var item = await GetFileMetadataAsync(id);
            if (item != null)
            {
                await _container.DeleteItemAsync<FileMetadata>(id, new PartitionKey(item.Category));
            }
        }
    }
}