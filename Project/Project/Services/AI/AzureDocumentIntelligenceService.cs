using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Project.Models;
using System.Text;

namespace Azure.WebApp.Services.AI
{
    public class AzureDocumentIntelligenceService : IAzureDocumentIntelligenceService
    {
        private readonly string _endpoint;
        private readonly string _apiKey;

        public AzureDocumentIntelligenceService(IConfiguration configuration)
        {
            _endpoint = configuration["AzureDocumentIntelligence:Endpoint"];
            _apiKey = configuration["AzureDocumentIntelligence:ApiKey"];
        }

        public async Task<string> AnalyzeDocumentAsync(BlobObject blob)
        {
            var client = GetDocumentIntelligenceClient();

            try
            {
                var operation = await client.AnalyzeDocumentFromUriAsync(
                    WaitUntil.Completed, 
                    "prebuilt-read", 
                    new Uri(blob.FileUri));
                
                var result = operation.Value;
                var extractedText = new StringBuilder();

                foreach (var page in result.Pages)
                {
                    foreach (var line in page.Lines)
                    {
                        extractedText.AppendLine(line.Content);
                    }
                }

                var fullText = extractedText.ToString().Trim();
                
                if (string.IsNullOrEmpty(fullText))
                {
                    return "Dokument nie zawiera tekstu";
                }

                // info
                var fileType = GetFileTypeName(Path.GetExtension(blob.Name));
                var pageCount = result.Pages.Count;
                var charCount = fullText.Length;
                
                // 7 pierwszych wyrazow
                var preview = GetFirstWords(fullText, 7);
                
                return $"{fileType}, {pageCount} str., ~{charCount} znaków. {preview}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Error analyzing document: {ex.Message}");
            }
        }

        private string GetFirstWords(string text, int wordCount)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "Brak treœci";
            }

            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length <= wordCount)
            {
                return string.Join(" ", words) + "...";
            }

            return string.Join(" ", words.Take(wordCount)) + "...";
        }

        private string GetFileTypeName(string extension)
        {
            return extension.ToLower() switch
            {
                ".pdf" => "PDF",
                ".docx" => "Word",
                ".doc" => "Word",
                ".txt" => "Tekst",
                ".xlsx" => "Excel",
                ".pptx" => "PowerPoint",
                _ => "Dokument"
            };
        }

        private DocumentAnalysisClient GetDocumentIntelligenceClient()
        {
            return new DocumentAnalysisClient(new Uri(_endpoint), new AzureKeyCredential(_apiKey));
        }
    }
}
