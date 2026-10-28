using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using RBI.DesignSystem.Components.Forms;
using RBI.DesignSystem.Components.Overlay;

namespace RBI.DesignSystem.Components.Menu;

/// <summary>A native horizontal application menubar with instance-local controlled/unbound selection.</summary>
public sealed class RbiMenubar : ComponentBase, IAsyncDisposable
{
    private readonly RbiMenubarContext _context;
    private ElementReference _root;
    private RbiAnchoredOverlayInterop? _interop;
    private IReadOnlyDictionary<string, object>? _safeAttributes;
    private bool _disposed, _initialized;
    private string? _nativeActive, _lastActive;
    [Inject] private IJSRuntime JavaScript { get; set; } = default!;
    /// <summary>Creates an instance-local menu selection scope.</summary>
    public RbiMenubar() => _context = new(() => EffectiveActive, ChangeActiveAsync);
    /// <summary>Localized accessible menubar name.</summary>
    [Parameter, EditorRequired] public string AccessibleLabel { get; set; } = string.Empty;
    /// <summary>Active menu identity when bound; initial/programmatic identity otherwise.</summary>
    [Parameter] public string? ActiveId { get; set; }
    /// <summary>Proposes active-menu changes without mutating supplied bound state.</summary>
    [Parameter] public EventCallback<string?> ActiveIdChanged { get; set; }
    /// <summary>Menus composing the bar.</summary>
    [Parameter, EditorRequired] public RenderFragment? ChildContent { get; set; }
    /// <summary>Additional scoped bar classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional bar attributes excluding owned ARIA semantics.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
    private string? EffectiveActive => _disposed ? null : ActiveIdChanged.HasDelegate ? ActiveId : _nativeActive;
    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(AccessibleLabel)) throw new ArgumentException("A menubar name is required.", nameof(AccessibleLabel));
        if (ChildContent is null) throw new ArgumentException("Menubar menus are required.", nameof(ChildContent));
        if (!_initialized || ActiveId != _lastActive) _nativeActive = ActiveId;
        _lastActive = ActiveId; _initialized = true;
        _safeAttributes = RbiFormUtilities.SafeAttributes(AdditionalAttributes, "class", "role", "aria-label", "aria-orientation");
    }
    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddMultipleAttributes(1, _safeAttributes);
        builder.AddAttribute(2, "class", $"rbi-menubar {Class}".TrimEnd());
        builder.AddAttribute(3, "role", "menubar");
        builder.AddAttribute(4, "aria-label", AccessibleLabel);
        builder.AddAttribute(5, "aria-orientation", "horizontal");
        builder.AddElementReferenceCapture(6, value => _root = value);
        builder.OpenComponent<CascadingValue<RbiMenubarContext>>(7);
        builder.AddAttribute(8, "Value", _context);
        builder.AddAttribute(9, "IsFixed", false);
        builder.AddAttribute(10, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }
    private async Task ChangeActiveAsync(string? value)
    {
        if (_disposed || EffectiveActive == value) return;
        if (!ActiveIdChanged.HasDelegate) _nativeActive = value;
        await ActiveIdChanged.InvokeAsync(value);
        if (!_disposed) StateHasChanged();
    }
    /// <summary>Receives native focus/keyboard menu selection proposals.</summary>
    [JSInvokable] public Task NativeActiveChanged(string? value) => ChangeActiveAsync(value);
    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed) return;
        _interop ??= new(JavaScript);
        await _interop.SyncMenubarAsync(_root, this, EffectiveActive);
    }
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_interop is not null) await _interop.DisposeAsync();
    }
}
