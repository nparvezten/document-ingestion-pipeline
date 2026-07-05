namespace DocumentIngestion.Api.Models;

public class DocumentLineItem
{
    public int Id { get; set; }
    public string DocumentId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public double UnitPrice { get; set; }
    public double TotalPrice { get; set; }

    // Navigation property (optional back-reference)
    // public Document? Document { get; set; }
}
