namespace RBI.ApiCore;

/// <summary>Bounded downloaded bytes and safe server attachment metadata.</summary>
public sealed class RbiApiFile
{
    internal RbiApiFile(byte[] content, string? contentType, string fileName)
    {
        Content = content;
        ContentType = contentType;
        FileName = fileName;
    }

    /// <summary>Gets the downloaded bytes. The transport never logs their contents.</summary>
    public byte[] Content { get; }
    /// <summary>Gets the response media type, or null when absent.</summary>
    public string? ContentType { get; }
    /// <summary>Gets a control-free basename; paths are removed and absent names become "download".</summary>
    public string FileName { get; }
}
