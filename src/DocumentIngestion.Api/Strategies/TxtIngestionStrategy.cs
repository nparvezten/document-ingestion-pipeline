using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DocumentIngestion.Api.Strategies;

public class TxtIngestionStrategy : IIngestionStrategy
{
    public string MimeType => "text/plain";

    public async Task<string> ExtractRawTextAsync(Stream fileStream)
    {
        if (fileStream == null)
            return string.Empty;

        // Ensure we read from the current position (which should be 0 unless seeking is handled outside)
        using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
        
        var sb = new StringBuilder();
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            sb.AppendLine(line);
        }

        return sb.ToString();
    }
}
