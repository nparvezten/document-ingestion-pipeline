using DocumentIngestion.Api.Models;

namespace DocumentIngestion.Api.Services;

public interface IMetadataExtractor
{
    ExtractedMetadata ExtractMetadata(string rawText);
}
