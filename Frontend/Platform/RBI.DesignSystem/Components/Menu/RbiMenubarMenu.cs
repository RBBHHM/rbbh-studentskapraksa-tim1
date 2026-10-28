using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RBI.DesignSystem.Components.Menu;

/// <summary>A stable named menubar entry reusing the native dropdown command scope.</summary>
public sealed class RbiMenubarMenu : ComponentBase, IDisposable
{
    [CascadingParameter] private RbiMenubarContext? Bar { get; set; }
    private string? _registered;
    private RbiMenubarContext? _registeredBar;
    private RenderFragment? _trigger;
    private IReadOnlyDictionary<string, object>? _surfaceAttributes;
    /// <summary>Unique stable identity within the menubar.</summary>
    [Parameter, EditorRequired] public string Id { get; set; } = string.Empty;
    /// <summary>Localized trigger and menu name.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;
    /// <summary>Commands, groups, selections and nested menus.</summary>
    [Parameter, EditorRequired] public RenderFragment? MenuContent { get; set; }
    /// <summary>Whether the menu cannot be opened.</summary>
    [Parameter] public bool Disabled { get; set; }
    /// <summary>Additional scoped popup classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Bar is null) throw new InvalidOperationException("Menubar entries require an RBI menubar.");
        if (string.IsNullOrWhiteSpace(Id) || Id.Any(char.IsWhiteSpace)) throw new ArgumentException("A whitespace-free menu identity is required.", nameof(Id));
        if (string.IsNullOrWhiteSpace(Label) || MenuContent is null) throw new ArgumentException("A menu label and commands are required.");
        if (_registered != Id || _registeredBar != Bar)
        {
            _registeredBar?.Unregister(_registered!);
            Bar.Register(Id);
            _registeredBar = Bar; _registered = Id;
            _surfaceAttributes = new Dictionary<string, object> { ["data-rbi-menubar-popup"] = "", ["data-rbi-menubar-id"] = Id };
        }
    }
    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<RbiDropdownMenu>(0);
        builder.AddAttribute(1, nameof(RbiDropdownMenu.AccessibleLabel), Label);
        builder.AddAttribute(2, nameof(RbiDropdownMenu.Open), !Disabled && Bar!.ActiveId == Id);
        builder.AddAttribute(3, nameof(RbiDropdownMenu.OpenChanged), EventCallback.Factory.Create<bool>(this, ChangedAsync));
        builder.AddAttribute(4, nameof(RbiDropdownMenu.TriggerContent), _trigger ??= BuildTrigger);
        builder.AddAttribute(5, nameof(RbiDropdownMenu.MenuContent), MenuContent);
        builder.AddAttribute(6, nameof(RbiDropdownMenu.Class), $"rbi-menubar-surface {Class}".TrimEnd());
        builder.AddAttribute(7, nameof(RbiDropdownMenu.AdditionalAttributes), _surfaceAttributes);
        builder.CloseComponent();
    }
    private void BuildTrigger(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "button");
        builder.AddAttribute(1, "type", "button");
        builder.AddAttribute(2, "role", "menuitem");
        builder.AddAttribute(3, "class", "rbi-menubar-trigger");
        builder.AddAttribute(4, "data-rbi-menubar-trigger", Id);
        builder.AddAttribute(5, "tabindex", -1);
        builder.AddAttribute(6, "disabled", Disabled);
        builder.AddAttribute(7, "aria-disabled", Disabled ? "true" : null);
        builder.AddAttribute(8, "aria-haspopup", "menu");
        builder.AddContent(9, Label);
        builder.CloseElement();
    }
    private Task ChangedAsync(bool open) => Disabled ? Task.CompletedTask : open ? Bar!.ChangeAsync(Id) : Bar!.ActiveId == Id ? Bar.ChangeAsync(null) : Task.CompletedTask;
    /// <inheritdoc />
    public void Dispose() => _registeredBar?.Unregister(_registered!);
}
