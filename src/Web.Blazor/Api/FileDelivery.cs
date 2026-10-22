using Microsoft.JSInterop;
using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public sealed class FileDelivery(IJSRuntime javascript)
{
    public async Task DownloadAsync(RbiApiFile file, CancellationToken token = default)
    {
        await using var module = await javascript.InvokeAsync<IJSObjectReference>("import", token, "./download.js");
        await using var stream = new MemoryStream(file.Content, writable: false);
        using var reference = new DotNetStreamReference(stream, leaveOpen: true);
        await module.InvokeVoidAsync("downloadFile", token, file.FileName, file.ContentType ?? "application/octet-stream", reference);
    }
}
