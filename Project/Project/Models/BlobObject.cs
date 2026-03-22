namespace Project.Models
{
    public class BlobObject
    {
        public string Name { get; set; }
        public string BlobName { get; set; }
        public string FileUri { get; set; }
        public string? Description { get; set; }
        public string? ContentType { get; set; }
        public long? FileSizeBytes { get; set; }
    }
}
