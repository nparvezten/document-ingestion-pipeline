using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace DocumentIngestion.Api.Strategies;

public class PdfIngestionStrategy : IIngestionStrategy
{
    public string MimeType => "application/pdf";

    public Task<string> ExtractRawTextAsync(Stream fileStream)
    {
        if (fileStream == null)
            return Task.FromResult(string.Empty);

        try
        {
            // Note: UglyToad.PdfPig's PdfDocument.Open requires a seekable stream.
            // We assume the caller provides a seekable stream (e.g. MemoryStream).
            using var pdfDocument = PdfDocument.Open(fileStream);
            var sb = new StringBuilder();

            foreach (var page in pdfDocument.GetPages())
            {
                // Group words by Y coordinate (rounded to 0 decimal places to handle slight baseline offsets)
                var lines = page.GetWords()
                    .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 0))
                    .OrderByDescending(g => g.Key);

                foreach (var lineGroup in lines)
                {
                    // Sort words from left to right on the line
                    var sortedWords = lineGroup.OrderBy(w => w.BoundingBox.Left);
                    string lineText = string.Join(" ", sortedWords.Select(w => w.Text));
                    if (!string.IsNullOrWhiteSpace(lineText))
                    {
                        sb.AppendLine(lineText);
                    }
                }
            }

            return Task.FromResult(sb.ToString());
        }
        catch (Exception ex)
        {
            throw new InvalidDataException("Failed to parse PDF document. The file may be corrupt or encrypted.", ex);
        }
    }
}
