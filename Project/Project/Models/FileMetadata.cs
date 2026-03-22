using Newtonsoft.Json;

namespace Project.Models
{
    public class FileMetadata
    {
        [JsonProperty("id")]
        public string Id { get; set; } // identyfikator w bazie
        
        [JsonProperty("blobName")]
        public string BlobName { get; set; } // nazwa pliku w blob storage
        
        [JsonProperty("fileUri")]
        public string FileUri { get; set; } // link do pliku
        
        [JsonProperty("uploadDate")]
        public DateTime UploadDate { get; set; } // data przeslania pliku
        
        [JsonProperty("fileName")]
        public string FileName { get; set; } // nazwa do edycji
        
        [JsonProperty("fileSizeBytes")]
        public long FileSizeBytes { get; set; } // rozmiar pliku
        
        [JsonProperty("contentType")]
        public string ContentType { get; set; } // np. "image/jpeg", "application/pdf", "text/plain"
        
        [JsonProperty("description")]
        public string? Description { get; set; } // opis
        
        [JsonProperty("fileExtension")]
        public string? FileExtension { get; set; } // rozszerzenie pliku
        
        [JsonProperty("category")]
        public string? Category { get; set; } // kat pliku (np. "dokument", "obraz", "wideo")
    }
}