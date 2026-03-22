using Project.Models;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Globalization;

namespace Azure.WebApp.Services.AI
{
    public class AzureComputerVisionService : IAzureComputerVisionService
    {
        private readonly string _subscriptionKey;
        private readonly string _endpoint;

        public AzureComputerVisionService(IConfiguration configuration)
        { 
            _subscriptionKey = configuration["AzureComputerVision:SubscriptionKey"];
            _endpoint = configuration["AzureComputerVision:Endpoint"];
        }
        
        public async Task<Dictionary<string, string>> AnalyzeBlobAsync(BlobObject blob)
        {
            var client = GetComputerVisionClient();

            var features = new List<VisualFeatureTypes?>()
            {
                // VisualFeatureTypes.Tags,
                VisualFeatureTypes.Description
            };

            try
            {
                var imageAnalysis = await client.AnalyzeImageAsync(blob.FileUri, visualFeatures: features);
                if (imageAnalysis == null)
                    return new Dictionary<string, string>();

                var result = new Dictionary<string, string>();

                /*
                foreach (var item in imageAnalysis.Tags)
                {
                    result.Add(item.Name.Replace(' ', '_').Replace('-', '_'), item.Confidence.ToString(CultureInfo.InvariantCulture));
                }
                */

                if (imageAnalysis.Description?.Captions != null && imageAnalysis.Description.Captions.Count > 0)
                {
                    result.Add("caption", imageAnalysis.Description.Captions[0].Text);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing image in computer vision: {ex.Message}");
            }
        }

        private ComputerVisionClient GetComputerVisionClient()
        {
            var client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(_subscriptionKey))
            {
                Endpoint = _endpoint
            };
            return client;
        }
    }
}
