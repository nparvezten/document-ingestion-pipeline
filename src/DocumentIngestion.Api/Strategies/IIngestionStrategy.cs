using System.IO;
using System.Threading.Tasks;

namespace DocumentIngestion.Api.Strategies;

public interface IIngestionStrategy
{
    string MimeType { get; }
    Task<string> ExtractRawTextAsync(Stream fileStream);
}
