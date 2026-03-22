using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Project.Models;

namespace Project.Services.Storage
{
    public class AzureBlobStorageService : IAzureBlobStorageService
    {

        private readonly string _storageConnectionString;
        private readonly string _storageContainerName;

        public AzureBlobStorageService(IConfiguration configuration)
        {
            // w nawiasie kwadratowym odwoalnie do slownikea z konfiguracja 
            _storageConnectionString = configuration["AzureBlobStorage:ConnectionString"];
            _storageContainerName = configuration["AzureBlobStorage:ContainerName"];
        }


        public async Task<List<BlobObject>> GetBlobsAsync()
        {
            var containerClient = GetBlobContainerClient();
            var blobs = new List<BlobObject>();
            var uri = containerClient.Uri.ToString();

            await foreach (var blob in containerClient.GetBlobsAsync())
            {
                var blobUri = $"{uri}/{blob.Name}";

                blobs.Add(new BlobObject
                {
                    Name = blob.Name,
                    FileUri = blobUri,
                    ContentType = blob.Properties.ContentType,
                    FileSizeBytes = blob.Properties.ContentLength
                });

            }

            return blobs;
        }

        public async Task<BlobObject> UploadAsync(IFormFile formFile)
        {
            var containerClient = GetBlobContainerClient();

            try
            {
                var client = containerClient.GetBlobClient(formFile.FileName);
                await using var data = formFile.OpenReadStream();
                
                var contentType = formFile.ContentType;

                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                };
                
                await client.UploadAsync(data, new BlobUploadOptions { HttpHeaders = blobHttpHeaders });

                // adres kontenera blobow
                var uri = containerClient.Uri.ToString();
                return new BlobObject
                {
                    Name = formFile.FileName,
                    FileUri = $"{uri}/{formFile.FileName}",
                    ContentType = contentType,
                    FileSizeBytes = formFile.Length
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error uploading blob: {ex.Message}");
            }
        }

        public async Task DeleteBlobAsync(string blobName)
        {
            var containerClient = GetBlobContainerClient();
            var client = containerClient.GetBlobClient(blobName);
            await client.DeleteIfExistsAsync();
        }

        private BlobContainerClient GetBlobContainerClient()
        {
            return new BlobContainerClient(_storageConnectionString, _storageContainerName);
        }

        /*
        private string GetCorrectContentType(string fileName, string providedContentType)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".md" => "text/markdown",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".mp4" => "video/mp4",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                _ => providedContentType ?? "application/octet-stream"
            };
        }

        // var contentType = GetCorrectContentType(formFile.FileName, formFile.ContentType);
        */
    }
}