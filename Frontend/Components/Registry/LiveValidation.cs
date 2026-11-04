using Microsoft.AspNetCore.Components.Forms;
using RBI.Forms;

namespace RBBH.RelatedPartiesRegister.Web.Components.Registry;

/// <summary>
/// Immediate feedback for a form: validates on every field change and shows messages for the fields the user has touched.
/// <see cref="Incomplete"/> blocks the primary action while a touched form is invalid.
/// </summary>
public sealed class LiveValidation : IDisposable
{
    private readonly EditContext _context;
    private readonly RbiServerValidation _server;
    private readonly Func<Dictionary<string, string[]>> _validate;
    private readonly Func<string, string> _translate;
    private readonly Action _changed;
    private readonly Dictionary<string, string[]> _related;
    private readonly HashSet<string> _touched = [];

    public LiveValidation(EditContext context, RbiServerValidation server, Func<Dictionary<string, string[]>> validate,
        Func<string, string> translate, Action changed, Dictionary<string, string[]>? related = null)
    {
        (_context, _server, _validate, _translate, _changed) = (context, server, validate, translate, changed);
        _related = related ?? [];
        _context.OnFieldChanged += FieldChanged;
    }

    public bool Incomplete => _touched.Count > 0 && _validate().Count > 0;

    private void FieldChanged(object? sender, FieldChangedEventArgs args)
    {
        _touched.Add(args.FieldIdentifier.FieldName);
        if (_related.TryGetValue(args.FieldIdentifier.FieldName, out var others)) foreach (var other in others) _touched.Add(other);
        var visible = _validate().Where(item => _touched.Contains(item.Key))
            .ToDictionary(item => item.Key, item => item.Value.Select(_translate).ToArray());
        _server.DisplayErrors(visible);
        _changed();
    }

    public void Dispose() => _context.OnFieldChanged -= FieldChanged;
}
