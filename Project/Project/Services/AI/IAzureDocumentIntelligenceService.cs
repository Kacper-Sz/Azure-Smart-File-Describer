using Project.Models;

namespace Azure.WebApp.Services.AI
{
    public interface IAzureDocumentIntelligenceService
    {
        Task<string> AnalyzeDocumentAsync(BlobObject blob);
    }
}
