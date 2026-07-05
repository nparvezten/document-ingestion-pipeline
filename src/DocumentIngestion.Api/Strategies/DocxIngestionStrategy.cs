using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DocumentIngestion.Api.Strategies;

public class DocxIngestionStrategy : IIngestionStrategy
{
    public string MimeType => "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public async Task<string> ExtractRawTextAsync(Stream fileStream)
    {
        if (fileStream == null)
            return string.Empty;

        try
        {
            // Open the DOCX package as a ZIP archive
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);
            var documentEntry = archive.GetEntry("word/document.xml");
            if (documentEntry == null)
            {
                throw new InvalidDataException("Invalid DOCX format: word/document.xml not found.");
            }

            using var entryStream = documentEntry.Open();
            using var xmlReader = XmlReader.Create(entryStream, new XmlReaderSettings { Async = true });
            
            var sb = new StringBuilder();
            bool insideTable = false;
            bool isFirstCellInRow = true;

            while (await xmlReader.ReadAsync())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    string localName = xmlReader.LocalName;

                    if (localName == "tbl")
                    {
                        insideTable = true;
                        sb.AppendLine(); // start table block
                    }
                    else if (localName == "tr")
                    {
                        sb.AppendLine(); // new table row
                        isFirstCellInRow = true;
                    }
                    else if (localName == "tc")
                    {
                        if (!isFirstCellInRow)
                        {
                            sb.Append('\t'); // separate table cells with tabs
                        }
                        isFirstCellInRow = false;
                    }
                    else if (localName == "p")
                    {
                        // Standard paragraph block (start a new line if not inside a table row)
                        if (!insideTable)
                        {
                            sb.AppendLine();
                        }
                    }
                    else if (localName == "t")
                    {
                        // Text element
                        string text = xmlReader.ReadString();
                        if (!string.IsNullOrEmpty(text))
                        {
                            sb.Append(text);
                        }
                    }
                }
                else if (xmlReader.NodeType == XmlNodeType.EndElement)
                {
                    if (xmlReader.LocalName == "tbl")
                    {
                        insideTable = false;
                        sb.AppendLine(); // end table block
                    }
                }
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            throw new InvalidDataException("Failed to parse DOCX document. Ensure it is a valid, uncorrupted DOCX package.", ex);
        }
    }
}
