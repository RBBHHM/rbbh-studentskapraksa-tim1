namespace RBI.DesignSystem.Components.Menu;

internal sealed class RbiMenubarContext(Func<string?> active, Func<string?, Task> change)
{
    private readonly HashSet<string> _identities = new(StringComparer.Ordinal);
    internal string? ActiveId => active();
    internal Task ChangeAsync(string? value) => change(value);
    internal void Register(string id)
    {
        if (!_identities.Add(id)) throw new ArgumentException("Menubar menu identities must be unique.", nameof(id));
    }
    internal void Unregister(string id) => _identities.Remove(id);
}
