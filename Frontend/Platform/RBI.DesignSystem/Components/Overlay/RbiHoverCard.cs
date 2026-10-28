using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using RBI.DesignSystem.Components.Forms;

namespace RBI.DesignSystem.Components.Overlay;

/// <summary>Delayed supplementary content anchored to one native trigger; never traps focus.</summary>
public sealed class RbiHoverCard : ComponentBase, IAsyncDisposable
{
    private readonly string _id = RbiFormUtilities.Id("hover-card");
    private ElementReference _root;
    private RbiAnchoredOverlayInterop? _interop;
    private IReadOnlyDictionary<string, object>? _safeAttributes;
    private bool _disposed, _initialized, _nativeOpen, _lastParameterOpen;
    [Inject] private IJSRuntime JavaScript { get; set; } = default!;
    /// <summary>Controlled open state when bound; initial/programmatic state otherwise.</summary>
    [Parameter] public bool Open { get; set; }
    /// <summary>Proposes native pointer/focus open-state changes.</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
    /// <summary>Localized name for supplementary content.</summary>
    [Parameter, EditorRequired] public string AccessibleLabel { get; set; } = string.Empty;
    /// <summary>Exactly one meaningful native element; its click behavior is preserved.</summary>
    [Parameter, EditorRequired] public RenderFragment? TriggerContent { get; set; }
    /// <summary>Supplementary content, including ordinary links when appropriate.</summary>
    [Parameter, EditorRequired] public RenderFragment? ChildContent { get; set; }
    /// <summary>Preferred placement, with native anchor/fallback collision handling.</summary>
    [Parameter] public RbiOverlayPlacement Placement { get; set; } = RbiOverlayPlacement.Bottom;
    /// <summary>Cross-axis alignment with the source trigger, defaulting to center.</summary>
    [Parameter] public RbiHoverCardAlignment Align { get; set; } = RbiHoverCardAlignment.Center;
    /// <summary>Pointer opening delay in milliseconds, matching the source default.</summary>
    [Parameter] public int OpenDelay { get; set; } = 700;
    /// <summary>Pointer closing grace in milliseconds, allowing entry into the anchored content.</summary>
    [Parameter] public int CloseDelay { get; set; } = 300;
    /// <summary>Additional scoped surface classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional surface attributes, excluding owned overlay semantics.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!_initialized || Open != _lastParameterOpen) _nativeOpen = Open;
        _lastParameterOpen = Open;
        _initialized = true;
        if (string.IsNullOrWhiteSpace(AccessibleLabel)) throw new ArgumentException("A supplementary content name is required.", nameof(AccessibleLabel));
        if (TriggerContent is null || ChildContent is null) throw new ArgumentException("A trigger and supplementary content are required.");
        if (OpenDelay < 0) throw new ArgumentOutOfRangeException(nameof(OpenDelay));
        if (CloseDelay < 0) throw new ArgumentOutOfRangeException(nameof(CloseDelay));
        if (!Enum.IsDefined(Placement)) throw new ArgumentOutOfRangeException(nameof(Placement));
        if (!Enum.IsDefined(Align)) throw new ArgumentOutOfRangeException(nameof(Align));
        _safeAttributes = RbiFormUtilities.SafeAttributes(AdditionalAttributes, "id", "class", "role", "popover", "aria-label", "data-rbi-anchored-surface", "data-rbi-open", "data-rbi-preferred-placement", "data-rbi-hover-align");
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "rbi-anchored-overlay-host");
        builder.AddAttribute(2, "data-rbi-hover-card-host", "");
        builder.AddElementReferenceCapture(3, value => _root = value);
        builder.AddContent(4, TriggerContent);
        builder.OpenElement(5, "div");
        builder.AddMultipleAttributes(6, _safeAttributes);
        builder.AddAttribute(7, "id", _id);
        builder.AddAttribute(8, "class", $"rbi-anchored-surface rbi-hover-card {Class}".TrimEnd());
        builder.AddAttribute(9, "role", "region");
        builder.AddAttribute(10, "aria-label", AccessibleLabel);
        builder.AddAttribute(11, "popover", "manual");
        builder.AddAttribute(12, "data-rbi-anchored-surface", "");
        builder.AddAttribute(13, "data-rbi-open", EffectiveOpen ? "true" : "false");
        builder.AddAttribute(14, "data-rbi-preferred-placement", Placement.ToString().ToLowerInvariant());
        builder.AddAttribute(15, "data-rbi-hover-align", Align.ToString().ToLowerInvariant());
        builder.AddContent(16, ChildContent);
        builder.CloseElement();
        builder.CloseElement();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed) return;
        _interop ??= new(JavaScript);
        await _interop.SyncHoverCardAsync(_root, this, EffectiveOpen, Placement, OpenDelay, CloseDelay, Align);
    }

    /// <summary>Receives browser hover/focus state proposals without mutating controlled state.</summary>
    [JSInvokable]
    public async Task NativeOpenChanged(bool open)
    {
        if (_disposed || EffectiveOpen == open) return;
        if (!OpenChanged.HasDelegate) _nativeOpen = open;
        await OpenChanged.InvokeAsync(open);
        if (!_disposed) StateHasChanged();
    }

    private bool EffectiveOpen => OpenChanged.HasDelegate ? Open : _nativeOpen;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_interop is not null) await _interop.DisposeAsync();
    }
}
