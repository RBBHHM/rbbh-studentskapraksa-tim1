using System.IO.Compression;

namespace RBBH.ConnectedParties.Helpers.Excel;

/// <summary>Bound ZIP expansion before parsing an untrusted OpenXML workbook.</summary>
public static class WorkbookEnvelope
{
    public static void Validate(Stream stream)
    {
        if (!stream.CanSeek || stream.Length > 10 * 1024 * 1024) throw new InvalidDataException("Workbook size limit.");
        stream.Position = 0;
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true))
        {
            if (archive.Entries.Count > 2000) throw new InvalidDataException("Workbook entry limit.");
            long total = 0;
            foreach (var entry in archive.Entries)
            {
                total = checked(total + entry.Length);
                if (entry.Length > 32 * 1024 * 1024 || total > 64 * 1024 * 1024)
                    throw new InvalidDataException("Workbook expansion limit.");
                if (entry.FullName.Contains("vbaProject", StringComparison.OrdinalIgnoreCase) ||
                    entry.FullName.StartsWith("xl/externalLinks/", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Active workbook content is not supported.");
            }
            if (archive.GetEntry("[Content_Types].xml") is null || archive.GetEntry("xl/workbook.xml") is null)
                throw new InvalidDataException("Workbook package is incomplete.");
        }
        stream.Position = 0;
    }
}
