namespace RBI.DesignSystem.Components.Menu;

internal sealed class RbiMenuContext(Func<bool, Task> closeAsync, Func<bool>? isOpen = null)
{
    internal Task CloseAsync(bool restoreFocus) => closeAsync(restoreFocus);
    internal bool IsOpen => isOpen?.Invoke() ?? true;
}
