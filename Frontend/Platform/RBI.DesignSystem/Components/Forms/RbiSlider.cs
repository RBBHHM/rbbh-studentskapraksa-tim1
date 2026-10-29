using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace RBI.DesignSystem.Components.Forms;

/// <summary>A source-styled single-thumb range with native keyboard and form binding.</summary>
public sealed class RbiSlider : InputBase<double>
{
    private readonly string _generatedId = RbiFormUtilities.Id("slider");
    private IReadOnlyDictionary<string, object>? _safeAttributes;

    [CascadingParameter] private RbiFieldContext? Field { get; set; }
    /// <summary>Stable control identifier; defaults to the field's identifier.</summary>
    [Parameter] public string? Id { get; set; }
    /// <summary>Accessible name when no visible associated label is provided.</summary>
    [Parameter] public string? AccessibleLabel { get; set; }
    /// <summary>Minimum finite value.</summary>
    [Parameter] public double Min { get; set; }
    /// <summary>Maximum finite value, strictly greater than the minimum.</summary>
    [Parameter] public double Max { get; set; } = 100;
    /// <summary>Positive finite step used by native keyboard and pointer interactions.</summary>
    [Parameter] public double Step { get; set; } = 1;
    /// <summary>Whether user interaction is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }
    /// <summary>Explicit invalid state, combined with field/form validation.</summary>
    [Parameter] public bool Invalid { get; set; }
    /// <summary>Optional localized spoken value, including units.</summary>
    [Parameter] public string? ValueText { get; set; }
    /// <summary>Native binding event; continuous input is the slider default.</summary>
    [Parameter] public RbiInputUpdateMode UpdateMode { get; set; } = RbiInputUpdateMode.Input;
    /// <summary>Signals a committed pointer/keyboard value, separately from continuous binding.</summary>
    [Parameter] public EventCallback<double> ValueCommitted { get; set; }

    private bool EffectiveDisabled => Disabled || Field?.Disabled == true;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!double.IsFinite(Min) || !double.IsFinite(Max) || !double.IsFinite(Max - Min) || Max <= Min) throw new ArgumentOutOfRangeException(nameof(Max), "The finite maximum must exceed the minimum.");
        if (!double.IsFinite(Step) || Step <= 0) throw new ArgumentOutOfRangeException(nameof(Step));
        if (!double.IsFinite(Value) || Value < Min || Value > Max) throw new ArgumentOutOfRangeException(nameof(Value));
        if (!Enum.IsDefined(UpdateMode)) throw new ArgumentOutOfRangeException(nameof(UpdateMode));
        _safeAttributes = RbiFormUtilities.SafeAttributes(AdditionalAttributes, "id", "class", "style", "type", "min", "max", "step", "value", "disabled", "aria-label", "aria-invalid", "aria-describedby", "aria-valuetext", "oninput", "onchange");
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var invalid = Invalid || Field?.Invalid == true || EditContext?.GetValidationMessages(FieldIdentifier).Any() == true;
        builder.OpenElement(0, "input");
        builder.AddMultipleAttributes(1, _safeAttributes);
        builder.AddAttribute(2, "id", Id ?? RbiFormUtilities.Attribute(AdditionalAttributes, "id") ?? Field?.ControlId ?? _generatedId);
        builder.AddAttribute(3, "type", "range");
        builder.AddAttribute(4, "class", $"rbi-slider {CssClass}".TrimEnd());
        builder.AddAttribute(5, "min", Number(Min));
        builder.AddAttribute(6, "max", Number(Max));
        builder.AddAttribute(7, "step", Number(Step));
        builder.AddAttribute(8, "value", Number(CurrentValue));
        builder.AddAttribute(9, "disabled", EffectiveDisabled);
        builder.AddAttribute(10, "aria-label", AccessibleLabel ?? RbiFormUtilities.Attribute(AdditionalAttributes, "aria-label"));
        builder.AddAttribute(11, "aria-invalid", invalid ? "true" : null);
        builder.AddAttribute(12, "aria-describedby", RbiFormUtilities.JoinIds(Field?.DescribedBy, RbiFormUtilities.Attribute(AdditionalAttributes, "aria-describedby")));
        builder.AddAttribute(13, "aria-valuetext", ValueText);
        builder.AddAttribute(14, "style", $"--rbi-slider-progress:{Number((CurrentValue - Min) / (Max - Min) * 100)}%;{RbiFormUtilities.Attribute(AdditionalAttributes, "style")}");
        if (UpdateMode == RbiInputUpdateMode.Input)
        {
            builder.AddAttribute(15, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, ChangeValue));
            builder.SetUpdatesAttributeName("value");
        }
        builder.AddAttribute(16, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, CommitValueAsync));
        builder.SetUpdatesAttributeName("value");
        builder.CloseElement();
    }

    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private void ChangeValue(ChangeEventArgs args)
    {
        if (EffectiveDisabled) return;
        CurrentValueAsString = args.Value?.ToString();
    }
    private async Task CommitValueAsync(ChangeEventArgs args)
    {
        if (EffectiveDisabled) return;
        ChangeValue(args);
        if (double.TryParse(args.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value) && value >= Min && value <= Max)
            await ValueCommitted.InvokeAsync(value);
    }

    /// <inheritdoc />
    protected override string? FormatValueAsString(double value) => Number(value);

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out double result, out string validationErrorMessage)
    {
        var valid = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) && double.IsFinite(result) && result >= Min && result <= Max;
        validationErrorMessage = valid ? string.Empty : $"The {DisplayName ?? FieldIdentifier.FieldName} field must be within the slider range.";
        return valid;
    }
}
