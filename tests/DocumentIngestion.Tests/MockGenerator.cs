using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace DocumentIngestion.Tests;

public static class MockGenerator
{
    public static readonly string InvoiceText = 
@"INVOICE
Invoice Date: 2026-06-15
Invoice Number: INV-987654
Due Date: 2026-07-15

Description                    Qty    Unit Price    Total
---------------------------------------------------------
Premium SaaS Subscription       1        150.00    150.00
Database Setup Consulting       5        120.00    600.00
Cloud Storage Add-on            2         25.00     50.00
---------------------------------------------------------
TOTAL AMOUNT: 800.00";

    public static void GenerateAll(string targetDirectory)
    {
        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        // 1. Generate Plain Text Invoice
        File.WriteAllText(Path.Combine(targetDirectory, "mock_invoice.txt"), InvoiceText, Encoding.UTF8);

        // 2. Generate PDF Invoice using UglyToad.PdfPig
        GeneratePdfInvoice(Path.Combine(targetDirectory, "mock_invoice.pdf"));

        // 3. Generate DOCX Ingestion Zip Mock
        GenerateDocxInvoice(Path.Combine(targetDirectory, "mock_invoice.docx"));

        // 4. Generate a tiny valid PNG file
        GeneratePngInvoice(Path.Combine(targetDirectory, "mock_invoice.png"));
    }

    private static void GeneratePdfInvoice(string filePath)
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(PageSize.A4);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        // Split text by lines and write them descending on the page coordinate space
        string[] lines = InvoiceText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        double yPos = 800; // start near top of A4 (which is 842 tall)

        foreach (var line in lines)
        {
            // Note: coordinates start from bottom-left (0,0) in PDF space
            page.AddText(line, 10, new PdfPoint(50, yPos), font);
            yPos -= 25; // move down for next line (proper line height spacing)
        }

        byte[] pdfBytes = builder.Build();
        File.WriteAllBytes(filePath, pdfBytes);
    }

    private static void GenerateDocxInvoice(string filePath)
    {
        // To verify DocxIngestionStrategy, we build a ZIP archive containing word/document.xml
        using var fileStream = File.Create(filePath);
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);
        
        var entry = archive.CreateEntry("word/document.xml");
        using var entryStream = entry.Open();
        using var writer = new StreamWriter(entryStream, Encoding.UTF8);

        // Construct standard OpenXML markup containing our text
        var xmlBuilder = new StringBuilder();
        xmlBuilder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        xmlBuilder.Append("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">");
        xmlBuilder.Append("<w:body>");

        // Add header fields
        AddDocxParagraph(xmlBuilder, "INVOICE");
        AddDocxParagraph(xmlBuilder, "Invoice Date: 2026-06-15");
        AddDocxParagraph(xmlBuilder, "Invoice Number: INV-987654");
        AddDocxParagraph(xmlBuilder, "Due Date: 2026-07-15");
        AddDocxParagraph(xmlBuilder, "");

        // Add table structure for validation
        xmlBuilder.Append("<w:tbl>");
        
        // Header Row
        AddDocxTableRow(xmlBuilder, "Description", "Qty", "Unit Price", "Total");
        
        // Data Rows
        AddDocxTableRow(xmlBuilder, "Premium SaaS Subscription", "1", "150.00", "150.00");
        AddDocxTableRow(xmlBuilder, "Database Setup Consulting", "5", "120.00", "600.00");
        AddDocxTableRow(xmlBuilder, "Cloud Storage Add-on", "2", "25.00", "50.00");
        
        xmlBuilder.Append("</w:tbl>");

        AddDocxParagraph(xmlBuilder, "");
        AddDocxParagraph(xmlBuilder, "TOTAL AMOUNT: 800.00");

        xmlBuilder.Append("</w:body>");
        xmlBuilder.Append("</w:document>");

        writer.Write(xmlBuilder.ToString());
    }

    private static void AddDocxParagraph(StringBuilder xml, string text)
    {
        xml.Append("<w:p><w:r><w:t>").Append(System.Security.SecurityElement.Escape(text)).Append("</w:t></w:r></w:p>");
    }

    private static void AddDocxTableRow(StringBuilder xml, params string[] cells)
    {
        xml.Append("<w:tr>");
        foreach (var cell in cells)
        {
            xml.Append("<w:tc><w:p><w:r><w:t>")
               .Append(System.Security.SecurityElement.Escape(cell))
               .Append("</w:t></w:r></w:p></w:tc>");
        }
        xml.Append("</w:tr>");
    }

    private static void GeneratePngInvoice(string filePath)
    {
        // Write standard 1x1 transparent PNG bytes
        byte[] pngBytes = {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
            0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, 0x00, 0x00, 0x00,
            0x0D, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
            0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49,
            0x45, 0x4E, 0x4D, 0xAE, 0x42, 0x60, 0x82
        };
        File.WriteAllBytes(filePath, pngBytes);
    }
}
