using System;
using System.IO;

namespace DocumentIngestion.Api.Strategies;

public static class FileTypeDetector
{
    public static string DetectMimeType(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (!stream.CanSeek)
            throw new InvalidOperationException("Stream must be seekable to perform type detection and subsequent parsing.");

        byte[] header = new byte[8];
        long originalPosition = stream.Position;
        
        int bytesRead = stream.Read(header, 0, header.Length);
        stream.Position = originalPosition; // Reset position so strategies can read it from the beginning

        if (bytesRead < 3)
        {
            return "text/plain"; // fallback for extremely short files
        }

        // PDF check: %PDF (hex: 25 50 44 46)
        if (bytesRead >= 4 && header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46)
        {
            return "application/pdf";
        }

        // DOCX check: Zip file signature PK.. (hex: 50 4B 03 04)
        if (bytesRead >= 4 && header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04)
        {
            return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        }

        // PNG check: (hex: 89 50 4E 47 0D 0A 1A 0A)
        if (bytesRead >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
            header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        {
            return "image/png";
        }

        // JPEG check: (hex: FF D8 FF)
        if (bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return "image/jpeg";
        }

        // TXT/CSV check or fallback
        // As a heuristic, check if the header contains any null bytes or non-ASCII control characters.
        // If it does, we treat it as an unknown binary file. Otherwise, we assume it is text/plain.
        for (int i = 0; i < bytesRead; i++)
        {
            byte b = header[i];
            // Allow tab, newline, carriage return, and standard printable characters.
            if (b == 0x00 || (b < 0x09 && b != 0x0A && b != 0x0D) || (b > 0x0D && b < 0x20 && b != 0x1B))
            {
                return "application/octet-stream"; // Binary fallback
            }
        }

        return "text/plain";
    }
}
