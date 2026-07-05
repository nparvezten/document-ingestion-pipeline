using System.Collections.Generic;

namespace DocumentIngestion.Api.Models;

public class ExtractedMetadata
{
    public string? DocumentDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public double? TotalAmount { get; set; }
    public List<ExtractedLineItem> LineItems { get; set; } = new();
    public Dictionary<string, string> Attributes { get; set; } = new();
}

public class ExtractedLineItem
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public double UnitPrice { get; set; }
    public double TotalPrice { get; set; }
}
