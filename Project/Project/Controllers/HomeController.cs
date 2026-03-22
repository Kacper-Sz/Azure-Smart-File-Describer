using Azure.WebApp.Services.AI;
using Microsoft.AspNetCore.Mvc;
using Project.Models;
using Project.Services.Database;
using Project.Services.Storage;
using System.Diagnostics;

namespace Project.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAzureBlobStorageService _azureBlobStorageService;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IAzureComputerVisionService _computerVisionService;
        private readonly IAzureDocumentIntelligenceService _documentIntelligenceService;

        public HomeController(
            IAzureBlobStorageService azureBlobStorageService, 
            ICosmosDbService cosmosDbService,
            IAzureComputerVisionService computerVisionService,
            IAzureDocumentIntelligenceService documentIntelligenceService
            )
        {
            _azureBlobStorageService = azureBlobStorageService;
            _cosmosDbService = cosmosDbService;
            _computerVisionService = computerVisionService;
            _documentIntelligenceService = documentIntelligenceService;
        }

        [HttpGet] 
        public async Task<IActionResult> Index(string sortOrder)
        {
            var blobs = await _azureBlobStorageService.GetBlobsAsync();
            var allMetadata = await _cosmosDbService.GetAllFileMetadataAsync();
            
            var combinedData = blobs.Select(blob =>
            {
                blob.BlobName = blob.Name;
                
                var metadata = allMetadata.FirstOrDefault(m => m.BlobName == blob.Name);
                if (metadata != null)
                {
                    blob.Description = metadata.Description;
                    blob.Name = metadata.FileName ?? blob.Name;
                }
                
                return blob;
            }).ToList();

            // sortowanie
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParam = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DateSortParam = sortOrder == "date" ? "date_desc" : "date";

            combinedData = sortOrder switch
            {
                "name_desc" => combinedData.OrderByDescending(b => b.Name).ToList(),
                "date" => combinedData.OrderBy(b => 
                {
                    var meta = allMetadata.FirstOrDefault(m => m.BlobName == b.BlobName);
                    return meta?.UploadDate ?? DateTime.MinValue;
                }).ToList(),
                "date_desc" => combinedData.OrderByDescending(b => 
                {
                    var meta = allMetadata.FirstOrDefault(m => m.BlobName == b.BlobName);
                    return meta?.UploadDate ?? DateTime.MinValue;
                }).ToList(),
                _ => combinedData.OrderBy(b => b.Name).ToList()
            };

            ViewBag.Blobs = combinedData;
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Upload(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                try
                {
                    var blobObject = await _azureBlobStorageService.UploadAsync(file);
                    
                    string description = null;
                    
                    // jak obraz to Computer Vision
                    if (file.ContentType.StartsWith("image/"))
                    {
                        try
                        {
                            var analysisResult = await _computerVisionService.AnalyzeBlobAsync(blobObject);
                            if (analysisResult.ContainsKey("caption"))
                            {
                                description = analysisResult["caption"];
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Computer Vision error: {ex.Message}");
                        }
                    }
                    // nie obraz
                    else
                    {
                        // plik tekstowy - podglad
                        if (file.ContentType.StartsWith("text/"))
                        {
                            try
                            {
                                using var reader = new StreamReader(file.OpenReadStream());
                                var content = await reader.ReadToEndAsync();
                                
                                var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                                var words = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                var wordCount = words.Length;
                                
                                var previewWords = words.Take(5);
                                var preview = string.Join(" ", previewWords);
                                
                                description = $"Plik tekstowy ({lines.Length} linii, ~{wordCount} s³ów): {preview}...";
                            }
                            catch
                            {
                                description = "Plik tekstowy";
                            }
                        }
                        // PDF itd - Document Intelligence
                        else
                        {
                            try
                            {
                                description = await _documentIntelligenceService.AnalyzeDocumentAsync(blobObject);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Document Intelligence error: {ex.Message}");
                                description = null;
                            }
                        }
                    }

                    var fileMetadata = new FileMetadata
                    {
                        BlobName = blobObject.Name,
                        FileUri = blobObject.FileUri,
                        FileName = file.FileName,
                        FileSizeBytes = file.Length,
                        ContentType = blobObject.ContentType,
                        Description = description,
                        FileExtension = Path.GetExtension(file.FileName),
                        Category = DetermineCategory(blobObject.ContentType)
                    };

                    await _cosmosDbService.AddFileMetadataAsync(fileMetadata);
                    
                    if (description != null)
                    {
                        TempData["Success"] = $"Plik przes³any! Wygenerowany opis: {description}";
                    }
                    else
                    {
                        TempData["Success"] = "Plik zosta³ pomyœlnie przes³any!";
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"B³¹d podczas przesy³ania pliku: {ex.Message}";
                }
            }
            else
            {
                TempData["Error"] = "Nie wybrano pliku lub plik jest pusty";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string name)
        {
            try
            {
                await _azureBlobStorageService.DeleteBlobAsync(name);
                
                var allMetadata = await _cosmosDbService.GetAllFileMetadataAsync();
                var fileMetadata = allMetadata.FirstOrDefault(f => f.BlobName == name);
                if (fileMetadata != null)
                {
                    await _cosmosDbService.DeleteFileMetadataAsync(fileMetadata.Id);
                }

                TempData["Success"] = "Plik zosta³ usuniêty!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"B³¹d podczas usuwania pliku: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string name)
        {
            var allMetadata = await _cosmosDbService.GetAllFileMetadataAsync();
            var fileMetadata = allMetadata.FirstOrDefault(f => f.BlobName == name);
            
            if (fileMetadata == null)
            {
                TempData["Error"] = "Nie znaleziono pliku";
                return RedirectToAction(nameof(Index));
            }

            return View(fileMetadata);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(FileMetadata model)
        {
            try
            {
                await _cosmosDbService.UpdateFileMetadataAsync(model.Id, model);
                TempData["Success"] = "Plik zosta³ zaktualizowany!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"B³¹d podczas aktualizacji pliku: {ex.Message}";
                return View(model);
            }
        }

        private string DetermineCategory(string contentType)
        {
            if (contentType.StartsWith("image/"))
                return "obraz";
            if (contentType.StartsWith("video/"))
                return "wideo";
            if (contentType.Contains("pdf") || contentType.Contains("document"))
                return "dokument";
            
            return "inne";
        }
    }
}
