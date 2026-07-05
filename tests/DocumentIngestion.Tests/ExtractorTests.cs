using DocumentIngestion.Api.Services;
using Xunit;

namespace DocumentIngestion.Tests;

public class ExtractorTests
{
    private readonly MetadataExtractor _extractor = new();

    [Fact]
    public void ExtractMetadata_TxtInvoice_ExtractsCorrectValues()
    {
        string rawText = MockGenerator.InvoiceText;
        var result = _extractor.ExtractMetadata(rawText);

        Assert.NotNull(result);
        Assert.Equal("2026-06-15", result.DocumentDate);
        Assert.Equal("INV-987654", result.ReferenceNumber);
        Assert.Equal(800.00, result.TotalAmount);
        
        Assert.NotNull(result.LineItems);
        Assert.Equal(3, result.LineItems.Count);

        // Assert premium subscription item
        var item1 = result.LineItems[0];
        Assert.Equal("Premium SaaS Subscription", item1.Description);
        Assert.Equal(1, item1.Quantity);
        Assert.Equal(150.00, item1.UnitPrice);
        Assert.Equal(150.00, item1.TotalPrice);

        // Assert database setup item
        var item2 = result.LineItems[1];
        Assert.Equal("Database Setup Consulting", item2.Description);
        Assert.Equal(5, item2.Quantity);
        Assert.Equal(120.00, item2.UnitPrice);
        Assert.Equal(600.00, item2.TotalPrice);

        // Assert cloud storage item
        var item3 = result.LineItems[2];
        Assert.Equal("Cloud Storage Add-on", item3.Description);
        Assert.Equal(2, item3.Quantity);
        Assert.Equal(25.00, item3.UnitPrice);
        Assert.Equal(50.00, item3.TotalPrice);
    }

    [Fact]
    public void ExtractMetadata_EmptyText_ReturnsEmptyResult()
    {
        var result = _extractor.ExtractMetadata(string.Empty);
        Assert.NotNull(result);
        Assert.Null(result.DocumentDate);
        Assert.Null(result.ReferenceNumber);
        Assert.Null(result.TotalAmount);
        Assert.Empty(result.LineItems);
    }

    [Fact]
    public void ExtractMetadata_AadharCardText_ExtractsAadharDetails()
    {
        string mockAadharText = @"
            GOVERNMENT OF INDIA
            To,
            Parvez Khan Abdul Rashid
            DOB: 14/09/1984
            Male
            
            1234 5678 9012
            Aadhaar - Mera Adhikar
        ";

        var result = _extractor.ExtractMetadata(mockAadharText);

        Assert.NotNull(result);
        Assert.Equal("1984-09-14", result.DocumentDate);
        Assert.Equal("1234 5678 9012", result.ReferenceNumber);
        Assert.Null(result.TotalAmount);
        Assert.Empty(result.LineItems);
    }
}
