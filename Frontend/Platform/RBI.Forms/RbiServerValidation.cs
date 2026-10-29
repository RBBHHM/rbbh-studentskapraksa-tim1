using Microsoft.AspNetCore.Components.Forms;

namespace RBI.Forms;

/// <summary>
/// Maps generic server validation messages into a standard Blazor <see cref="EditContext"/>.
/// </summary>
public sealed class RbiServerValidation : IDisposable
{
    private readonly EditContext editContext;
    private readonly ValidationMessageStore messages;
    private readonly FieldIdentifier formField;
    private bool disposed;

    /// <summary>
    /// Creates server validation storage for an edit context.
    /// </summary>
    public RbiServerValidation(EditContext editContext)
    {
        ArgumentNullException.ThrowIfNull(editContext);

        this.editContext = editContext;
        messages = new ValidationMessageStore(editContext);
        formField = new FieldIdentifier(editContext.Model, string.Empty);
        editContext.OnFieldChanged += HandleFieldChanged;
        editContext.OnValidationRequested += HandleValidationRequested;
    }

    /// <summary>
    /// Replaces previous server errors with errors keyed by top-level model property name.
    /// An empty key represents a form-level error.
    /// </summary>
    public void DisplayErrors(IReadOnlyDictionary<string, string[]> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        ThrowIfDisposed();

        messages.Clear();
        foreach (var (fieldName, fieldMessages) in errors)
        {
            if (fieldMessages is null) continue;
            var nonEmptyMessages = fieldMessages.Where(message => !string.IsNullOrWhiteSpace(message));
            messages.Add(Field(fieldName), nonEmptyMessages);
        }
        editContext.NotifyValidationStateChanged();
    }

    /// <summary>
    /// Adds a form-level server error without inventing a model property name.
    /// </summary>
    public void DisplayFormError(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        ThrowIfDisposed();

        messages.Add(formField, error);
        editContext.NotifyValidationStateChanged();
    }

    /// <summary>
    /// Clears all server validation errors.
    /// </summary>
    public void Clear()
    {
        ThrowIfDisposed();
        messages.Clear();
        editContext.NotifyValidationStateChanged();
    }

    /// <summary>
    /// Clears server validation errors for one top-level model property.
    /// </summary>
    public void Clear(string fieldName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        ThrowIfDisposed();

        messages.Clear(Field(fieldName));
        editContext.NotifyValidationStateChanged();
    }

    private FieldIdentifier Field(string? fieldName) => string.IsNullOrWhiteSpace(fieldName)
        ? formField
        : new FieldIdentifier(editContext.Model, fieldName);

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs args)
    {
        messages.Clear(args.FieldIdentifier);
        editContext.NotifyValidationStateChanged();
    }

    private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs args) => messages.Clear();

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposed, this);

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed) return;
        editContext.OnFieldChanged -= HandleFieldChanged;
        editContext.OnValidationRequested -= HandleValidationRequested;
        disposed = true;
    }
}
