using System.IO;
using System.Text;
using DocumentIngestion.Api.Strategies;
using Xunit;

namespace DocumentIngestion.Tests;

public class DetectorTests
{
    [Fact]
    public void DetectMimeType_Pdf_ReturnsPdfMime()
    {
        byte[] pdfHeader = { 0x25, 0x50, 0x44, 0x46, 0x31, 0x2E, 0x34, 0x0A }; // %PDF1.4
        using var stream = new MemoryStream(pdfHeader);
        string mime = FileTypeDetector.DetectMimeType(stream);
        Assert.Equal("application/pdf", mime);
        Assert.Equal(0, stream.Position); // verify stream position reset
    }

    [Fact]
    public void DetectMimeType_Docx_ReturnsDocxMime()
    {
        byte[] docxHeader = { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x08, 0x00 }; // PK..
        using var stream = new MemoryStream(docxHeader);
        string mime = FileTypeDetector.DetectMimeType(stream);
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", mime);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public void DetectMimeType_Png_ReturnsPngMime()
    {
        byte[] pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using var stream = new MemoryStream(pngHeader);
        string mime = FileTypeDetector.DetectMimeType(stream);
        Assert.Equal("image/png", mime);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public void DetectMimeType_Txt_ReturnsTxtMime()
    {
        byte[] txtHeader = Encoding.UTF8.GetBytes("INVOICE\nDate: 2026-06-15");
        using var stream = new MemoryStream(txtHeader);
        string mime = FileTypeDetector.DetectMimeType(stream);
        Assert.Equal("text/plain", mime);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public void DetectMimeType_BinaryOctet_ReturnsOctetStream()
    {
        byte[] binaryHeader = { 0x00, 0x01, 0x02, 0x03, 0x04, 0xFF, 0x0A, 0x0D };
        using var stream = new MemoryStream(binaryHeader);
        string mime = FileTypeDetector.DetectMimeType(stream);
        Assert.Equal("application/octet-stream", mime);
        Assert.Equal(0, stream.Position);
    }
}
