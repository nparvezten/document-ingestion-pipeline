using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DocumentIngestion.Api.Strategies;
using Xunit;

namespace DocumentIngestion.Tests;

public class StrategyTests : IDisposable
{
    private readonly string _tempTestDir;

    public StrategyTests()
    {
        // Setup a temporary workspace directory for test execution files
        _tempTestDir = Path.Combine(Directory.GetCurrentDirectory(), "temp_test_assets");
        MockGenerator.GenerateAll(_tempTestDir);
    }

    public void Dispose()
    {
        // Cleanup generated test files
        if (Directory.Exists(_tempTestDir))
        {
            try
            {
                Directory.Delete(_tempTestDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors during testing
            }
        }
    }

    [Fact]
    public async Task TxtIngestionStrategy_ExtractsRawText()
    {
        var strategy = new TxtIngestionStrategy();
        string mockTxtPath = Path.Combine(_tempTestDir, "mock_invoice.txt");
        
        using var stream = File.OpenRead(mockTxtPath);
        string result = await strategy.ExtractRawTextAsync(stream);

        Assert.Contains("INVOICE", result);
        Assert.Contains("TOTAL AMOUNT: 800.00", result);
    }

    [Fact]
    public async Task PdfIngestionStrategy_ExtractsRawText()
    {
        var strategy = new PdfIngestionStrategy();
        string mockPdfPath = Path.Combine(_tempTestDir, "mock_invoice.pdf");
        
        using var stream = File.OpenRead(mockPdfPath);
        string result = await strategy.ExtractRawTextAsync(stream);

        Assert.Contains("INVOICE", result);
        Assert.Contains("Invoice Date: 2026-06-15", result);
        Assert.Contains("INV-987654", result);
        Assert.Contains("TOTAL AMOUNT: 800.00", result);
    }

    [Fact]
    public async Task DocxIngestionStrategy_ExtractsRawTextWithTabs()
    {
        var strategy = new DocxIngestionStrategy();
        string mockDocxPath = Path.Combine(_tempTestDir, "mock_invoice.docx");
        
        using var stream = File.OpenRead(mockDocxPath);
        string result = await strategy.ExtractRawTextAsync(stream);

        Assert.Contains("INVOICE", result);
        Assert.Contains("Premium SaaS Subscription\t1\t150.00\t150.00", result);
        Assert.Contains("Database Setup Consulting\t5\t120.00\t600.00", result);
        Assert.Contains("Cloud Storage Add-on\t2\t25.00\t50.00", result);
        Assert.Contains("TOTAL AMOUNT: 800.00", result);
    }

    [Fact]
    public async Task ImageIngestionStrategy_ExecutesWithoutCrashing()
    {
        var strategy = new ImageIngestionStrategy("image/png");
        string mockPngPath = Path.Combine(_tempTestDir, "mock_invoice.png");

        using var stream = File.OpenRead(mockPngPath);
        
        // This should run the OCR pathway or fallback gracefully without raising an exception.
        string result = await strategy.ExtractRawTextAsync(stream);

        Assert.NotNull(result);
        // Fallback or actual OCR result should contain invoice contents
        Assert.Contains("INVOICE", result);
    }

    [Fact]
    public void GenerateBaselineMockDocumentsInRoot()
    {
        // Directory.GetCurrentDirectory() yields: tests/DocumentIngestion.Tests/bin/Debug/net9.0
        // Four directories up gets us to the workspace root tests/ folder
        string rootTestsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..");
        MockGenerator.GenerateAll(rootTestsPath);
        
        Assert.True(File.Exists(Path.Combine(rootTestsPath, "mock_invoice.txt")));
        Assert.True(File.Exists(Path.Combine(rootTestsPath, "mock_invoice.pdf")));
        Assert.True(File.Exists(Path.Combine(rootTestsPath, "mock_invoice.docx")));
        Assert.True(File.Exists(Path.Combine(rootTestsPath, "mock_invoice.png")));
    }
}
