using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using RBI.DesignSystem.Components.Menu;

namespace RBI.DesignSystem.Components.Overlay;

internal sealed class RbiAnchoredOverlayInterop(IJSRuntime javaScript, string modulePath) : IAsyncDisposable
{
    public RbiAnchoredOverlayInterop(IJSRuntime javaScript) : this(javaScript, "./_content/RBI.DesignSystem/js/rbi-anchored-overlay.js") { }
    private IJSObjectReference? _module;
    private IDisposable? _dotNetReference;
    private ElementReference _root;
    private bool _connected, _disposed;
    private Task _operation = Task.CompletedTask;
    private Task? _disposal;
    private readonly string _connectionId = Guid.NewGuid().ToString("N");

    // Serializing DOM calls prevents duplicate imports/connects and lets disposal
    // settle in-flight work before releasing the module and callback reference.
    private Task RunAsync(Func<IJSObjectReference, Task> action) =>
        _disposed ? Task.CompletedTask : _operation = RunCoreAsync(_operation, action);

    private async Task RunCoreAsync(Task previous, Func<IJSObjectReference, Task> action)
    {
        await previous;
        if (_disposed) return;
        _module ??= await javaScript.InvokeAsync<IJSObjectReference>("import", modulePath);
        if (!_disposed) await action(_module);
    }

    internal Task SyncTooltipAsync(ElementReference root, RbiOverlayPlacement placement, int delay, bool enabled = true) => RunAsync(async module =>
    {
        _root = root;
        if (!_connected)
        {
            _connected = true;
            await module.InvokeVoidAsync("connectTooltip", root, placement.ToString().ToLowerInvariant(), delay, enabled, _connectionId);
        }
        else await module.InvokeVoidAsync("syncTooltip", root, placement.ToString().ToLowerInvariant(), delay, enabled);
    });

    internal Task SyncPopoverAsync<T>(ElementReference root, T owner, bool open, RbiOverlayPlacement placement) where T : class => RunAsync(async module =>
    {
        _root = root;
        if (!_connected)
        {
            var reference = DotNetObjectReference.Create(owner);
            _dotNetReference = reference;
            _connected = true;
            await module.InvokeVoidAsync("connectPopover", root, reference, open, placement.ToString().ToLowerInvariant(), _connectionId);
        }
        else await module.InvokeVoidAsync("syncPopover", root, open, placement.ToString().ToLowerInvariant());
    });

    internal Task SyncHoverCardAsync<T>(ElementReference root, T owner, bool open, RbiOverlayPlacement placement, int openDelay, int closeDelay, RbiHoverCardAlignment align = RbiHoverCardAlignment.Center) where T : class => RunAsync(async module =>
    {
        _root = root;
        if (!_connected)
        {
            var reference = DotNetObjectReference.Create(owner);
            _dotNetReference = reference;
            _connected = true;
            await module.InvokeVoidAsync("connectHoverCard", root, reference, open, placement.ToString().ToLowerInvariant(), openDelay, closeDelay, _connectionId, align.ToString().ToLowerInvariant());
        }
        else await module.InvokeVoidAsync("syncHoverCard", root, open, placement.ToString().ToLowerInvariant(), openDelay, closeDelay, align.ToString().ToLowerInvariant());
    });

    internal Task SyncMenuAsync<T>(ElementReference root, T owner, bool open, RbiOverlayPlacement placement, string invocationMode, RbiMenuAlignment align = RbiMenuAlignment.Center) where T : class => RunAsync(async module =>
    {
        _root = root;
        if (!_connected)
        {
            var reference = DotNetObjectReference.Create(owner);
            _dotNetReference = reference;
            _connected = true;
            await module.InvokeVoidAsync("connectMenu", root, reference, open, placement.ToString().ToLowerInvariant(), invocationMode, _connectionId, align.ToString().ToLowerInvariant());
        }
        else await module.InvokeVoidAsync("syncMenu", root, open, placement.ToString().ToLowerInvariant(), align.ToString().ToLowerInvariant());
    });

    internal Task SyncSubMenuAsync<T>(ElementReference root, T owner, bool open) where T : class => RunAsync(async module =>
    {
        _root = root;
        if (!_connected)
        {
            var reference = DotNetObjectReference.Create(owner);
            _dotNetReference = reference;
            _connected = true;
            await module.InvokeVoidAsync("connectSubMenu", root, reference, open, _connectionId);
        }
        else await module.InvokeVoidAsync("syncSubMenu", root, open);
    });

    internal Task SyncMenubarAsync<T>(ElementReference root, T owner, string? activeId) where T : class => RunAsync(async module =>
    {
        _root = root;
        if (!_connected)
        {
            var reference = DotNetObjectReference.Create(owner);
            _dotNetReference = reference;
            _connected = true;
            await module.InvokeVoidAsync("connectMenubar", root, reference, activeId, _connectionId);
        }
        else await module.InvokeVoidAsync("syncMenubar", root, activeId);
    });

    internal Task FocusMenuAsync(string target) => RunAsync(async module =>
    {
        if (_connected) await module.InvokeVoidAsync("focusMenu", _root, target);
    });

    internal Task SyncNavigationAsync<T>(ElementReference root, T owner, string? activeId) where T : class => RunAsync(async module =>
    {
        _root = root;
        if (!_connected)
        {
            _dotNetReference = DotNetObjectReference.Create(owner);
            _connected = true;
            await module.InvokeVoidAsync("connectNavigation", root, _dotNetReference, activeId, _connectionId);
        }
        else await module.InvokeVoidAsync("syncNavigation", root, activeId);
    });

    internal Task SyncControlsAsync<T>(ElementReference root, T owner, string kind, object settings) where T : class => RunAsync(async module =>
    {
        _root = root;
        if (!_connected)
        {
            _dotNetReference = DotNetObjectReference.Create(owner);
            _connected = true;
            await module.InvokeVoidAsync("connectControls", root, _dotNetReference, kind, settings, _connectionId);
        }
        else await module.InvokeVoidAsync("syncControls", root, settings);
    });

    public ValueTask DisposeAsync() => new(_disposal ??= DisposeCoreAsync());

    private async Task DisposeCoreAsync()
    {
        _disposed = true;
        try { await _operation; }
        catch (JSDisconnectedException) { }
        // Observe, but do not replay, an already-cancelled synchronization during
        // teardown. The original caller still observes cancellation; real faults propagate.
        catch (OperationCanceledException) when (_operation.IsCanceled) { }
        finally
        {
            try
            {
                if (_module is not null && _connected)
                {
                    try { await _module.InvokeVoidAsync("disconnect", _root, _connectionId); }
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
}
