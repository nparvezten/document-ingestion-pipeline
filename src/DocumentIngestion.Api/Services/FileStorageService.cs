using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DocumentIngestion.Api.Models;

namespace DocumentIngestion.Api.Services;

public class FileStorageService
{
    private readonly string _outputDirectory;

    public FileStorageService() : this(Path.Combine(Directory.GetCurrentDirectory(), "outputs"))
    {
    }

    public FileStorageService(string outputDirectory)
    {
        _outputDirectory = outputDirectory;
        EnsureDirectoryExists();
    }

    public async Task<string> SaveMinifiedJsonAsync(string documentId, ExtractedMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(documentId))
            throw new ArgumentException("Document ID cannot be null or empty.", nameof(documentId));

        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        EnsureDirectoryExists();

        var options = new JsonSerializerOptions
        {
            WriteIndented = false, // Minified JSON
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        string json = JsonSerializer.Serialize(metadata, options);
        string fileName = $"{documentId}_metadata.json";
        string filePath = Path.Combine(_outputDirectory, fileName);

        await File.WriteAllTextAsync(filePath, json);
        return filePath;
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
    }
}
