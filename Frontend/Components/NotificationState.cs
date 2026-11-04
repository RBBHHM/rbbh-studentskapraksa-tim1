namespace RBBH.RelatedPartiesRegister.Web.Components;

/// <summary>Per-circuit unread count shared by the header bell and the inbox page.</summary>
public sealed class NotificationState
{
    public int Unread { get; private set; }
    public event Action? Changed;
    public void Set(int unread)
    {
        var value = Math.Max(0, unread);
        if (value == Unread) return;
        Unread = value; Changed?.Invoke();
    }
}
