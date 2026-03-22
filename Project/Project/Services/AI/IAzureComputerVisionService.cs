using Project.Models;

namespace Azure.WebApp.Services.AI
{
    public interface IAzureComputerVisionService
    {
        Task<Dictionary<string, string>> AnalyzeBlobAsync (BlobObject blob);

    }
}
