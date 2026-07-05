using System;
using System.Collections.Generic;
using System.Linq;

namespace DocumentIngestion.Api.Strategies;

public class IngestionStrategyResolver
{
    private readonly IEnumerable<IIngestionStrategy> _strategies;

    public IngestionStrategyResolver(IEnumerable<IIngestionStrategy> strategies)
    {
        _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
    }

    public IIngestionStrategy Resolve(string mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            throw new ArgumentException("Mime type cannot be null or empty.", nameof(mimeType));
        }

        string cleanedMime = mimeType.Trim().ToLowerInvariant();

        // Find the strategy matching the MIME type exactly
        var strategy = _strategies.FirstOrDefault(s => s.MimeType.Equals(cleanedMime, StringComparison.OrdinalIgnoreCase));
        
        if (strategy == null)
        {
            // If it is jpeg or jpg and we have a png strategy, we can use it as they are both image strategies
            if (cleanedMime == "image/jpeg" || cleanedMime == "image/jpg")
            {
                strategy = _strategies.FirstOrDefault(s => s is ImageIngestionStrategy);
            }
        }

        if (strategy == null)
        {
            throw new NotSupportedException($"The file format/MIME type '{mimeType}' is not supported by the ingestion engine.");
        }

        return strategy;
    }
}
