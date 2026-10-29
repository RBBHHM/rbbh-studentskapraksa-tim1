using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace RBI.DesignSystem.Components.Overlay;

internal sealed class RbiModalInterop(IJSRuntime javaScript) : IAsyncDisposable
{
    private const string ModulePath = "./_content/RBI.DesignSystem/js/rbi-dialog.js";
    private readonly string _connectionId = Guid.NewGuid().ToString("N");
    private IJSObjectReference? _module;
    private IDisposable? _dotNetReference;
    private ElementReference _element;
    private bool _connected;
    private bool _disposed;
    private Task _operation = Task.CompletedTask;
    private Task? _disposal;

    internal Task SyncAsync<T>(ElementReference element, T owner, bool open, string? initialFocusSelector, bool dismissOnBackdrop, bool dismissOnEscape)
        where T : class
        => _disposed ? Task.CompletedTask : _operation = SyncCoreAsync(_operation, element, owner, open, initialFocusSelector, dismissOnBackdrop, dismissOnEscape);

    private async Task SyncCoreAsync<T>(Task previous, ElementReference element, T owner, bool open, string? initialFocusSelector, bool dismissOnBackdrop, bool dismissOnEscape) where T : class
    {
        await previous;
        if (_disposed) return;
        _element = element;
        _module ??= await javaScript.InvokeAsync<IJSObjectReference>("import", ModulePath);
        if (_disposed) return;
        if (!_connected)
        {
            var reference = DotNetObjectReference.Create(owner);
            _dotNetReference = reference;
            _connected = true;
            await _module.InvokeVoidAsync("connect", element, reference, dismissOnBackdrop, dismissOnEscape, _connectionId);
        }

        await _module.InvokeVoidAsync("sync", element, open, initialFocusSelector, dismissOnBackdrop, dismissOnEscape);
    }

    public ValueTask DisposeAsync() => new(_disposal ??= DisposeCoreAsync());

    private async Task DisposeCoreAsync()
    {
        _disposed = true;
        try { await _operation; }
        catch (JSDisconnectedException) { }
        finally { await ReleaseAsync(); }
    }

    private async Task ReleaseAsync()
    {
        try
        {
            if (_module is not null && _connected)
            {
                try { await _module.InvokeVoidAsync("disconnect", _element, _connectionId); }
                catch (JSDisconnectedException) { }
            }
        }
        finally
        {
            try
            {
                if (_module is not null)
                {
                    try { await _module.DisposeAsync(); }
                    catch (JSDisconnectedException) { }
                }
            }
            finally { _dotNetReference?.Dispose(); }
        }
    }
}
