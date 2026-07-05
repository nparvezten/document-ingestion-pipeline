using System;
using System.Collections.Generic;

namespace DocumentIngestion.Api.Models;

public class Document
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string? ExtractedDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public double? TotalAmount { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public List<DocumentLineItem> LineItems { get; set; } = new();
}
