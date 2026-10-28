using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using RBI.DesignSystem.Components.Icons;

namespace RBI.DesignSystem.Components.Forms;

/// <summary>A single native one-time-code input presented as source-backed grouped slots.</summary>
public sealed class RbiInputOtp : InputBase<string?>, IAsyncDisposable
{
    private readonly string _generatedId = RbiFormUtilities.Id("otp");
    private ElementReference _root, _input;
    private Task _operation = Task.CompletedTask;
    private Task<IJSObjectReference>? _moduleImport;
    private Task? _disposal;
    private IJSObjectReference? _module;
    private DotNetObjectReference<RbiInputOtp>? _reference;
    private IReadOnlyDictionary<string, object>? _safeAttributes;
    private bool _connected, _disposed, _focused, _valueSeen, _completionPending;
    private string _renderedValue = string.Empty;
    private int _selectionStart, _selectionEnd;
    [Inject] private IJSRuntime JavaScript { get; set; } = default!;
    [CascadingParameter] private RbiFieldContext? Field { get; set; }
    /// <summary>Stable native input identifier, defaulting to the enclosing field.</summary>
    [Parameter] public string? Id { get; set; }
    /// <summary>Accessible name when no visible associated label is supplied.</summary>
    [Parameter] public string? AccessibleLabel { get; set; }
    /// <summary>Code length, between one and 32 slots.</summary>
    [Parameter] public int Length { get; set; } = 6;
    /// <summary>Slots per visual group, with a separator between groups.</summary>
    [Parameter] public int GroupSize { get; set; } = 3;
    /// <summary>Allowed input characters; Any preserves the unpatterned source behavior.</summary>
    [Parameter] public RbiOtpCharacterSet CharacterSet { get; set; }
    /// <summary>Whether native editing is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }
    /// <summary>Whether editing is unavailable while selection/copy remains possible.</summary>
    [Parameter] public bool ReadOnly { get; set; }
    /// <summary>Whether native form submission requires a complete code.</summary>
    [Parameter] public bool Required { get; set; }
    /// <summary>Explicit invalid state, combined with field and form validation.</summary>
    [Parameter] public bool Invalid { get; set; }
    /// <summary>Signals an accepted incomplete-to-complete transition of the rendered value (typed, pasted or programmatic); an initially complete value does not signal.</summary>
    [Parameter] public EventCallback<string?> Completed { get; set; }
    /// <summary>Decorative, nonfocusable slot template; native editing and semantics remain component-owned.</summary>
    [Parameter] public RenderFragment<RbiOtpSlot>? SlotTemplate { get; set; }
    /// <summary>Optional decorative composition of typed slot states into caller-owned groups. Retains one native input.</summary>
    [Parameter] public RenderFragment<IReadOnlyList<RbiOtpSlot>>? ChildContent { get; set; }
    /// <summary>Transforms clipboard text before insertion and whole-value pattern validation, as in the source.</summary>
    [Parameter] public Func<string, string>? PasteTransformer { get; set; }
    /// <summary>Optional decorative separator; defaults to the source Minus geometry.</summary>
    [Parameter] public RenderFragment? Separator { get; set; }
    /// <summary>Additional scoped wrapper classes.</summary>
    [Parameter] public string? Class { get; set; }

    private bool EffectiveDisabled => Disabled || Field?.Disabled == true;
    private bool EffectiveInvalid => Invalid || Field?.Invalid == true || EditContext?.GetValidationMessages(FieldIdentifier).Any() == true;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Length is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(Length));
        if (GroupSize < 1) throw new ArgumentOutOfRangeException(nameof(GroupSize));
        if (!Enum.IsDefined(CharacterSet)) throw new ArgumentOutOfRangeException(nameof(CharacterSet));
        if (!Accepts(Value)) throw new ArgumentException("The supplied code does not match the configured length/character set.", nameof(Value));
        // Completion follows the rendered value, as in the source: any accepted incomplete-to-complete transition (typed, pasted or programmatic) signals it,
        // an initially complete value does not, and a proposal the owner rejects never reaches the rendered value.
        var rendered = Value ?? string.Empty;
        if (_valueSeen && _renderedValue.Length < Length && rendered.Length == Length) _completionPending = true;
        _renderedValue = rendered;
        _valueSeen = true;
        _safeAttributes = RbiFormUtilities.SafeAttributes(AdditionalAttributes, "id", "class", "type", "value", "pattern", "maxlength", "minlength", "inputmode", "autocomplete", "disabled", "readonly", "required", "aria-label", "aria-invalid", "aria-describedby", "oninput");
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var value = CurrentValue ?? string.Empty;
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", $"rbi-otp {Class}".TrimEnd());
        builder.AddAttribute(2, "data-disabled", EffectiveDisabled ? "true" : null);
        builder.AddAttribute(3, "data-invalid", EffectiveInvalid ? "true" : null);
        builder.AddElementReferenceCapture(4, value => _root = value);
        if (ChildContent is not null)
        {
            var slots = Enumerable.Range(0, Length).Select(index =>
            {
                var character = index < value.Length ? value[index] : (char?)null;
                var active = _focused && (_selectionStart == _selectionEnd ? index == Math.Min(_selectionStart, Length - 1) : index >= _selectionStart && index < _selectionEnd);
                return new RbiOtpSlot(index, character, active, active && character is null);
            }).ToArray();
            builder.AddContent(5, ChildContent(Array.AsReadOnly(slots)));
        }
        else for (var group = 0; group < Length; group += GroupSize)
        {
            builder.OpenRegion(5);
            if (group > 0)
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "rbi-otp__separator");
                builder.AddAttribute(2, "aria-hidden", "true");
                if (Separator is not null) builder.AddContent(3, Separator);
                else { builder.OpenComponent<RbiIcon>(4); builder.AddAttribute(5, nameof(RbiIcon.Name), RbiIconName.Minus); builder.CloseComponent(); }
                builder.CloseElement();
            }
            builder.OpenElement(6, "div");
            builder.SetKey(group);
            builder.AddAttribute(7, "class", "rbi-otp__group");
            builder.AddAttribute(8, "aria-hidden", "true");
            for (var index = group; index < Math.Min(group + GroupSize, Length); index++)
            {
                builder.OpenRegion(9);
                var character = index < value.Length ? value[index] : (char?)null;
                var active = _focused && (_selectionStart == _selectionEnd ? index == Math.Min(_selectionStart, Length - 1) : index >= _selectionStart && index < _selectionEnd);
                var slot = new RbiOtpSlot(index, character, active, active && character is null);
                builder.OpenElement(0, "span");
                builder.SetKey(index);
                builder.AddAttribute(1, "class", "rbi-otp__slot");
                builder.AddAttribute(2, "data-rbi-otp-slot", index);
                builder.AddAttribute(3, "data-active", active ? "true" : null);
                if (SlotTemplate is not null) builder.AddContent(4, SlotTemplate(slot));
                else
                {
                    builder.AddContent(5, character?.ToString());
                    if (slot.HasCaret) { builder.OpenElement(6, "span"); builder.AddAttribute(7, "class", "rbi-otp__caret"); builder.CloseElement(); }
                }
                builder.CloseElement();
                builder.CloseRegion();
            }
            builder.CloseElement();
            builder.CloseRegion();
        }
        builder.OpenElement(6, "input");
        builder.AddMultipleAttributes(7, _safeAttributes);
        builder.AddAttribute(8, "id", Id ?? RbiFormUtilities.Attribute(AdditionalAttributes, "id") ?? Field?.ControlId ?? _generatedId);
        builder.AddAttribute(9, "class", $"rbi-otp__input {CssClass}".TrimEnd());
        builder.AddAttribute(10, "type", "text");
        builder.AddAttribute(11, "inputmode", CharacterSet is RbiOtpCharacterSet.Letters or RbiOtpCharacterSet.Alphanumeric ? "text" : "numeric");
        builder.AddAttribute(12, "autocomplete", "one-time-code");
        builder.AddAttribute(13, "maxlength", Length);
        builder.AddAttribute(14, "minlength", Required || Field?.Required == true ? Length : null);
        builder.AddAttribute(15, "value", CurrentValueAsString);
        builder.AddAttribute(16, "disabled", EffectiveDisabled);
        builder.AddAttribute(17, "readonly", ReadOnly);
        builder.AddAttribute(18, "required", Required || Field?.Required == true);
        builder.AddAttribute(19, "aria-label", AccessibleLabel ?? RbiFormUtilities.Attribute(AdditionalAttributes, "aria-label"));
        builder.AddAttribute(20, "aria-invalid", EffectiveInvalid ? "true" : null);
        builder.AddAttribute(21, "aria-describedby", RbiFormUtilities.JoinIds(Field?.DescribedBy, RbiFormUtilities.Attribute(AdditionalAttributes, "aria-describedby")));
        builder.AddAttribute(22, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, ChangeValueAsync));
        builder.SetUpdatesAttributeName("value");
        builder.AddAttribute(23, "pattern", RbiFormUtilities.Attribute(AdditionalAttributes, "pattern") ?? (CharacterSet switch
        {
            RbiOtpCharacterSet.Digits => "^[0-9]+$",
            RbiOtpCharacterSet.Letters => "^[a-zA-Z]+$",
            RbiOtpCharacterSet.Alphanumeric => "^[a-zA-Z0-9]+$",
            _ => null,
        }));
        builder.AddAttribute(24, "data-rbi-otp-transform-paste", PasteTransformer is not null ? "true" : null);
        builder.AddElementReferenceCapture(25, value => _input = value);
        builder.CloseElement();
        builder.CloseElement();
    }

    private bool Accepts(string? value) => value is null || value.Length <= Length && value.All(character => CharacterSet switch
    {
        RbiOtpCharacterSet.Digits => character is >= '0' and <= '9',
        RbiOtpCharacterSet.Letters => character is >= 'a' and <= 'z' or >= 'A' and <= 'Z',
        RbiOtpCharacterSet.Alphanumeric => character is >= '0' and <= '9' or >= 'a' and <= 'z' or >= 'A' and <= 'Z',
        _ => true,
    });
    private Task ChangeValueAsync(ChangeEventArgs args)
    {
        if (EffectiveDisabled || ReadOnly) return Task.CompletedTask;
        var next = args.Value?.ToString();
        if (!Accepts(next)) return Task.CompletedTask;
        CurrentValueAsString = next;
        return Task.CompletedTask;
    }
    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out string? result, out string validationErrorMessage)
    {
        result = value;
        validationErrorMessage = string.Empty;
        return true;
    }
    /// <summary>Receives native selection/focus properties; no code value is transported by this callback.</summary>
    [JSInvokable]
    public Task NativeSelectionChanged(int start, int end, bool focused)
    {
        if (_disposed || start < 0 || end < start || end > Length) return Task.CompletedTask;
        if (_selectionStart == start && _selectionEnd == end && _focused == focused) return Task.CompletedTask;
        _selectionStart = start; _selectionEnd = end; _focused = focused;
        return InvokeAsync(StateHasChanged);
    }
    /// <summary>Transforms a browser paste proposal; a disposed or noneditable control cancels it.</summary>
    [JSInvokable]
    public string? TransformPaste(string text) => _disposed || EffectiveDisabled || ReadOnly ? null : PasteTransformer?.Invoke(text) ?? text;
    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender) => _disposed ? Task.CompletedTask : _operation = SyncAsync(_operation);

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (!_completionPending) return;
        _completionPending = false;
        if (Value is { } completed) await Completed.InvokeAsync(completed);
    }
    private async Task SyncAsync(Task previous)
    {
        await previous;
        if (_disposed) return;
        _moduleImport ??= JavaScript.InvokeAsync<IJSObjectReference>("import", "./_content/RBI.DesignSystem/js/rbi-native-controls.js").AsTask();
        _module = await _moduleImport;
        if (_disposed) return;
        if (!_connected)
        {
            _reference = DotNetObjectReference.Create(this);
            _connected = true;
            await _module.InvokeVoidAsync("connectOtp", _root, _input, _reference, _generatedId);
        }
        else await _module.InvokeVoidAsync("syncOtp", _root);
    }
    /// <inheritdoc />
    public ValueTask DisposeAsync() => new(_disposal ??= DisposeCoreAsync());
    private async Task DisposeCoreAsync()
    {
        _disposed = true;
        ((IDisposable)this).Dispose();
        try { await _operation; }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) when (_operation.IsCanceled) { }
        finally
        {
            try
            {
                if (_module is not null && _connected)
                {
                    try { await _module.InvokeVoidAsync("disconnectOtp", _root, _generatedId); }
                    catch (JSDisconnectedException) { }
                }
            }
            finally
            {
                try { if (_module is not null) await _module.DisposeAsync(); }
                catch (JSDisconnectedException) { }
                finally { _reference?.Dispose(); }
            }
        }
    }
}
