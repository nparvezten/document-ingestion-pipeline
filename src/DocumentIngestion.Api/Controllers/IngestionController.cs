using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentIngestion.Api.Data;
using DocumentIngestion.Api.Models;
using DocumentIngestion.Api.Services;
using DocumentIngestion.Api.Strategies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DocumentIngestion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngestionController : ControllerBase
{
    private readonly IngestionStrategyResolver _strategyResolver;
    private readonly IMetadataExtractor _metadataExtractor;
    private readonly FileStorageService _fileStorageService;
    private readonly SecurityHelper _securityHelper;
    private readonly AppDbContext _dbContext;

    public IngestionController(
        IngestionStrategyResolver strategyResolver,
        IMetadataExtractor metadataExtractor,
        FileStorageService fileStorageService,
        SecurityHelper securityHelper,
        AppDbContext dbContext)
    {
        _strategyResolver = strategyResolver;
        _metadataExtractor = metadataExtractor;
        _fileStorageService = fileStorageService;
        _securityHelper = securityHelper;
        _dbContext = dbContext;
    }

    [HttpPost("upload")]
    [DisableRequestSizeLimit] // Prevent IIS/Kestrel file upload limits
    public async Task<IActionResult> IngestDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new IngestionResult
            {
                Success = false,
                ErrorMessage = "No file was uploaded or the uploaded file is empty."
            });
        }

        string? tempFilePath = null;
        Stream? seekableStream = null;

        try
        {
            // Memory efficient stream buffer copy:
            // For files <= 5MB, load into MemoryStream.
            // For files > 5MB, spool to a temporary disk stream to prevent RAM spikes.
            const long maxMemoryThreshold = 5 * 1024 * 1024; // 5MB

            if (file.Length <= maxMemoryThreshold)
            {
                seekableStream = new MemoryStream();
                await file.CopyToAsync(seekableStream);
                seekableStream.Position = 0;
            }
            else
            {
                tempFilePath = Path.Combine(Path.GetTempPath(), $"ingest_spool_{Guid.NewGuid()}.tmp");
                seekableStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);
                await file.CopyToAsync(seekableStream);
                seekableStream.Position = 0;
            }

            // 1. Detect file type using magic bytes (header inspection)
            string detectedMimeType = FileTypeDetector.DetectMimeType(seekableStream);

            // 2. Resolve Strategy
            IIngestionStrategy strategy;
            try
            {
                strategy = _strategyResolver.Resolve(detectedMimeType);
            }
            catch (NotSupportedException nse)
            {
                return BadRequest(new IngestionResult
                {
                    FileName = file.FileName,
                    FileType = detectedMimeType,
                    Success = false,
                    ErrorMessage = nse.Message
                });
            }

            // 3. Extract Raw Text (Stream-based processing)
            string rawText;
            try
            {
                rawText = await strategy.ExtractRawTextAsync(seekableStream);
            }
            catch (Exception ex)
            {
                // Graceful isolation for parsing/corruption errors
                Console.WriteLine($"[PARSING ERROR] File: {file.FileName}, MIME: {detectedMimeType}. Error: {ex.Message}");
                return UnprocessableEntity(new IngestionResult
                {
                    FileName = file.FileName,
                    FileType = detectedMimeType,
                    Success = false,
                    ErrorMessage = $"Failed to parse file structures: {ex.Message}"
                });
            }

            // 4. Heuristic Metadata Extraction & Normalization
            ExtractedMetadata extractedMetadata = _metadataExtractor.ExtractMetadata(rawText);

            // Encrypt reference number (holds Aadhar ID or Invoice numbers) and compute blind index
            string? encryptedRef = string.IsNullOrWhiteSpace(extractedMetadata.ReferenceNumber)
                ? null
                : _securityHelper.Encrypt(extractedMetadata.ReferenceNumber);

            string? blindIndex = string.IsNullOrWhiteSpace(extractedMetadata.ReferenceNumber)
                ? null
                : _securityHelper.ComputeBlindIndex(extractedMetadata.ReferenceNumber);

            // 5. Database Save (SQLite)
            var document = new Document
            {
                FileName = file.FileName,
                FileType = detectedMimeType,
                ExtractedDate = extractedMetadata.DocumentDate,
                ReferenceNumber = encryptedRef,
                ReferenceNumberBlindIndex = blindIndex,
                TotalAmount = extractedMetadata.TotalAmount,
                AttributesJson = System.Text.Json.JsonSerializer.Serialize(extractedMetadata.Attributes),
                ProcessedAt = DateTime.UtcNow
            };

            document.LineItems = extractedMetadata.LineItems.Select(li => new DocumentLineItem
            {
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                TotalPrice = li.TotalPrice
            }).ToList();

            _dbContext.Documents.Add(document);
            await _dbContext.SaveChangesAsync();

            // 6. JSON File Storage Egress (minified layout on disk)
            string storagePath = await _fileStorageService.SaveMinifiedJsonAsync(document.Id, extractedMetadata);

            return Ok(new IngestionResult
            {
                DocumentId = document.Id,
                FileName = file.FileName,
                FileType = detectedMimeType,
                Success = true,
                ExtractedMetadata = extractedMetadata,
                StoragePath = storagePath
            });
        }
        catch (Exception ex)
        {
            // Global controller level catch for robust isolation
            Console.WriteLine($"[PIPELINE EXCEPTION] Failed ingesting document {file.FileName}. Error: {ex}");
            return StatusCode(500, new IngestionResult
            {
                FileName = file.FileName,
                Success = false,
                ErrorMessage = $"An unexpected error occurred in the ingestion pipeline: {ex.Message}"
            });
        }
        finally
        {
            if (seekableStream != null)
            {
                await seekableStream.DisposeAsync();
            }

            // Cleanup spool file if it was created
            if (tempFilePath != null && System.IO.File.Exists(tempFilePath))
            {
                try
                {
                    System.IO.File.Delete(tempFilePath);
                }
                catch
                {
                    // Ignore failures during OS temporary file cleanup
                }
            }
        }
    }
}
