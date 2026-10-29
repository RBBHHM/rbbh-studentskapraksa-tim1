using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using RBI.DesignSystem.Components.Forms;
using RBI.DesignSystem.Components.Icons;
using RBI.DesignSystem.Components.Overlay;

namespace RBI.DesignSystem.Components.Menu;

/// <summary>An anchored, nested native menu using the owning menu's close/focus scope.</summary>
public sealed class RbiSubMenu : ComponentBase, IAsyncDisposable
{
    private readonly string _id = RbiFormUtilities.Id("submenu");
    private readonly RbiMenuContext _context;
    private ElementReference _root;
    private RbiAnchoredOverlayInterop? _interop;
    private IReadOnlyDictionary<string, object>? _safeAttributes;
    private RenderFragment? _surface;
    private bool _disposed, _initialized, _nativeOpen, _lastParameterOpen;
    [Inject] private IJSRuntime JavaScript { get; set; } = default!;
    [CascadingParameter] private RbiMenuContext? Parent { get; set; }

    /// <summary>Creates an instance-local nested command scope.</summary>
    public RbiSubMenu() => _context = new(CloseMenusAsync, () => EffectiveOpen);
    /// <summary>Localized visible trigger and submenu name.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;
    /// <summary>Commands composing the submenu; ordinary and selection items may be mixed.</summary>
    [Parameter, EditorRequired] public RenderFragment? MenuContent { get; set; }
    /// <summary>Optional decorative leading icon.</summary>
    [Parameter] public RenderFragment? Icon { get; set; }
    /// <summary>Controlled open state when bound; initial/programmatic state otherwise.</summary>
    [Parameter] public bool Open { get; set; }
    /// <summary>Proposes pointer/keyboard open-state changes.</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
    /// <summary>Whether opening is unavailable.</summary>
    [Parameter] public bool Disabled { get; set; }
    /// <summary>Indents the trigger to align with items that carry a leading selection indicator.</summary>
    [Parameter] public bool Inset { get; set; }
    /// <summary>Additional scoped surface classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional surface attributes, excluding owned naming and menu semantics.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private bool OwnOpen => OpenChanged.HasDelegate ? Open : _nativeOpen;
    private bool EffectiveOpen => !_disposed && !Disabled && Parent?.IsOpen == true && OwnOpen;
    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Parent is null) throw new InvalidOperationException("A submenu requires an owning RBI menu.");
        if (string.IsNullOrWhiteSpace(Label)) throw new ArgumentException("A submenu label is required.", nameof(Label));
        if (MenuContent is null) throw new ArgumentException("Submenu commands are required.", nameof(MenuContent));
        if (!_initialized || Open != _lastParameterOpen) _nativeOpen = Open;
        _lastParameterOpen = Open;
        _initialized = true;
        _safeAttributes = RbiFormUtilities.SafeAttributes(AdditionalAttributes, "id", "class", "role", "popover", "aria-label", "aria-labelledby", "data-rbi-anchored-surface", "data-rbi-open");
    }
    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "rbi-anchored-overlay-host");
        builder.AddAttribute(2, "data-rbi-menu-sub-host", "");
        builder.AddElementReferenceCapture(3, value => _root = value);
        builder.OpenElement(4, "button");
        builder.AddAttribute(5, "id", _id + "-trigger");
        builder.AddAttribute(6, "type", "button");
        builder.AddAttribute(7, "role", "menuitem");
        builder.AddAttribute(8, "class", Inset ? "rbi-menu-item rbi-menu-sub-trigger rbi-menu-item--inset" : "rbi-menu-item rbi-menu-sub-trigger");
        builder.AddAttribute(9, "tabindex", -1);
        builder.AddAttribute(10, "aria-haspopup", "menu");
        builder.AddAttribute(11, "aria-expanded", EffectiveOpen ? "true" : "false");
        builder.AddAttribute(12, "aria-controls", _id);
        builder.AddAttribute(13, "aria-disabled", Disabled ? "true" : null);
        builder.AddAttribute(14, "data-rbi-menu-item", "");
        builder.AddAttribute(15, "data-rbi-menu-text", Label);
        if (Icon is not null)
        {
            builder.OpenElement(16, "span"); builder.AddAttribute(17, "class", "rbi-menu-item__icon"); builder.AddAttribute(18, "aria-hidden", "true"); builder.AddContent(19, Icon); builder.CloseElement();
        }
        builder.OpenElement(20, "span"); builder.AddAttribute(21, "class", "rbi-menu-item__label"); builder.AddContent(22, Label); builder.CloseElement();
        builder.OpenElement(23, "span"); builder.AddAttribute(24, "class", "rbi-menu-sub__chevron"); builder.AddAttribute(25, "aria-hidden", "true");
        builder.OpenComponent<RbiIcon>(26); builder.AddAttribute(27, nameof(RbiIcon.Name), RbiIconName.ChevronRight); builder.CloseComponent(); builder.CloseElement();
        builder.CloseElement();
        builder.OpenComponent<CascadingValue<RbiMenuContext>>(28);
        builder.AddAttribute(29, "Value", _context);
        builder.AddAttribute(30, "IsFixed", true);
        builder.AddAttribute(31, "ChildContent", _surface ??= BuildSurface);
        builder.CloseComponent();
        builder.CloseElement();
    }
    private void BuildSurface(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddMultipleAttributes(1, _safeAttributes);
        builder.AddAttribute(2, "id", _id);
        builder.AddAttribute(3, "class", $"rbi-anchored-surface rbi-menu rbi-menu-sub-surface {Class}".TrimEnd());
        builder.AddAttribute(4, "role", "menu");
        builder.AddAttribute(5, "aria-labelledby", _id + "-trigger");
        builder.AddAttribute(6, "popover", "manual");
        builder.AddAttribute(7, "data-rbi-anchored-surface", "");
        builder.AddAttribute(8, "data-rbi-open", EffectiveOpen ? "true" : "false");
        builder.AddContent(9, MenuContent);
        builder.CloseElement();
    }
    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed) return;
        _interop ??= new(JavaScript);
        await _interop.SyncSubMenuAsync(_root, this, EffectiveOpen);
    }
    /// <summary>Receives native nested-menu proposals without mutating supplied bound state.</summary>
    [JSInvokable]
    public async Task NativeOpenChanged(bool open)
    {
        if (_disposed || open && (Disabled || Parent?.IsOpen != true) || OwnOpen == open) return;
        if (!OpenChanged.HasDelegate) _nativeOpen = open;
        await OpenChanged.InvokeAsync(open);
        if (!_disposed) StateHasChanged();
    }
    private async Task CloseMenusAsync(bool restoreFocus)
    {
        await NativeOpenChanged(false);
        if (!_disposed && Parent is not null) await Parent.CloseAsync(restoreFocus);
    }
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_interop is not null) await _interop.DisposeAsync();
    }
}
