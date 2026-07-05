namespace DocumentIngestion.Api.Models;

public class IngestionResult
{
    public string DocumentId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public ExtractedMetadata? ExtractedMetadata { get; set; }
    public string? StoragePath { get; set; }
}
