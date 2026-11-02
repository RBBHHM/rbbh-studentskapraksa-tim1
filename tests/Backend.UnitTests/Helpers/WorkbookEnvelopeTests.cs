using System.IO.Compression;
using RBBH.ConnectedParties.Helpers.Excel;

namespace UnitTests.Helpers;

public sealed class WorkbookEnvelopeTests
{
    [Theory]
    [InlineData("xl/externalLinks/externalLink1.xml")]
    [InlineData("xl/vbaProject.bin")]
    public void ActiveContentIsRejectedBeforeWorkbookParsing(string path)
    {
        using var stream = Package(path, 1);
        Assert.Throws<InvalidDataException>(() => WorkbookEnvelope.Validate(stream));
    }
    [Fact]
    public void HighlyCompressedOversizedXmlIsRejectedByExpandedLength()
    {
        using var stream = Package("xl/worksheets/sheet1.xml", 33 * 1024 * 1024);
        Assert.True(stream.Length < 10 * 1024 * 1024);
        Assert.Throws<InvalidDataException>(() => WorkbookEnvelope.Validate(stream));
    }
    [Fact]
    public void ValidEnvelopeIsLeftOpenAtTheBeginning()
    {
        using var stream = Package("xl/worksheets/sheet1.xml", 8);
        WorkbookEnvelope.Validate(stream); Assert.True(stream.CanRead); Assert.Equal(0, stream.Position);
    }
    private static MemoryStream Package(string path, int length)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            archive.CreateEntry("[Content_Types].xml"); archive.CreateEntry("xl/workbook.xml");
            using var data = archive.CreateEntry(path, CompressionLevel.Fastest).Open();
            var block = new byte[8192];
            for (var remaining = length; remaining > 0; remaining -= Math.Min(block.Length, remaining)) data.Write(block, 0, Math.Min(block.Length, remaining));
        }
        stream.Position = 0; return stream;
    }
}
