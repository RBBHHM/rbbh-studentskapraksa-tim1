namespace RBI.DesignSystem.Components.Feedback;

/// <summary>Source-demonstrated notification treatments.</summary>
public enum RbiToastTone { Neutral, Success, Warning, Danger }

/// <summary>A caller-owned notification action, without persistence or application services.</summary>
public sealed record RbiToastAction(string Label, Func<Task> InvokeAsync);

/// <summary>Immutable notification presentation. Null duration keeps a message until dismissed.</summary>
public sealed record RbiToastMessage(string Title)
{
    public string? Description { get; init; }
    public RbiToastTone Tone { get; init; }
    public TimeSpan? Duration { get; init; } = TimeSpan.FromSeconds(4);
    public RbiToastAction? Action { get; init; }
    public RbiToastAction? Cancel { get; init; }
}

/// <summary>Immutable identity and presentation of a queued notification.</summary>
public sealed record RbiToastEntry(Guid Id, RbiToastMessage Message);

/// <summary>Caller-owned, in-memory UI queue. Dispose when its application/circuit scope ends.</summary>
public sealed class RbiToastQueue : IDisposable
{
    private sealed class Pending(RbiToastEntry entry, TimeSpan? remaining)
    {
        internal readonly RbiToastEntry Entry = entry;
        internal TimeSpan? Remaining = remaining;
        internal long Started;
        internal ITimer? Timer;
        internal int Generation;
    }
    private sealed class PauseLease(RbiToastQueue owner) : IDisposable
    {
        private RbiToastQueue? _owner = owner;
        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.Resume();
    }
    private readonly object _gate = new();
    private readonly TimeProvider _time;
    private readonly List<Pending> _items = [];
    private int _pauses;
    private bool _disposed;

    /// <summary>Optional clock supports deterministic tests; production uses the system clock.</summary>
    public RbiToastQueue(TimeProvider? timeProvider = null) => _time = timeProvider ?? TimeProvider.System;
    /// <summary>Raised outside the queue lock. UI subscribers must marshal to their renderer.</summary>
    public event Action? Changed;
    /// <summary>Oldest-to-newest immutable snapshot, safe to enumerate while timers change.</summary>
    public IReadOnlyList<RbiToastEntry> Entries { get { lock (_gate) return Array.AsReadOnly(_items.Select(item => item.Entry).ToArray()); } }

    /// <summary>Publish a validated UI message and return its dismissal identity.</summary>
    public Guid Publish(RbiToastMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (string.IsNullOrWhiteSpace(message.Title)) throw new ArgumentException("A notification title is required.", nameof(message));
        if (!Enum.IsDefined(message.Tone)) throw new ArgumentOutOfRangeException(nameof(message));
        if (message.Duration is { } duration && (duration <= TimeSpan.Zero || duration.TotalMilliseconds > uint.MaxValue - 1)) throw new ArgumentOutOfRangeException(nameof(message));
        foreach (var action in new[] { message.Action, message.Cancel })
            if (action is not null && (string.IsNullOrWhiteSpace(action.Label) || action.InvokeAsync is null)) throw new ArgumentException("Actions require a label and callback.", nameof(message));
        var pending = new Pending(new(Guid.NewGuid(), message), message.Duration);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _items.Add(pending);
            if (_pauses == 0) Start(pending);
        }
        Changed?.Invoke();
        return pending.Entry.Id;
    }
    /// <summary>Dismiss once; false means the identity is no longer queued.</summary>
    public bool Dismiss(Guid id)
    {
        lock (_gate)
        {
            var item = _items.Find(item => item.Entry.Id == id);
            if (item is null || _disposed) return false;
            Stop(item); _items.Remove(item);
        }
        Changed?.Invoke(); return true;
    }
    /// <summary>Pause all expiration clocks. Overlapping pointer/focus leases resume only after the last lease ends.</summary>
    public IDisposable Pause()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_pauses++ == 0)
                foreach (var item in _items)
                {
                    if (item.Remaining is { } remaining)
                        item.Remaining = TimeSpan.FromTicks(Math.Max(0, (remaining - _time.GetElapsedTime(item.Started)).Ticks));
                    Stop(item);
                }
        }
        return new PauseLease(this);
    }
    internal IDisposable? PauseIfAlive()
    {
        lock (_gate) return _disposed ? null : Pause();
    }
    private void Resume()
    {
        lock (_gate)
        {
            if (_disposed || _pauses == 0 || --_pauses > 0) return;
            foreach (var item in _items) Start(item);
        }
    }
    private static void Stop(Pending item) { item.Generation++; item.Timer?.Dispose(); item.Timer = null; }
    private void Start(Pending item)
    {
        if (item.Remaining is not { } remaining) return;
        item.Started = _time.GetTimestamp();
        var generation = ++item.Generation;
        // Create disabled, assign, then arm: even a zero-due callback can safely dispose its timer.
        item.Timer = _time.CreateTimer(_ => Expire(item, generation), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        item.Timer.Change(remaining, Timeout.InfiniteTimeSpan);
    }
    private void Expire(Pending item, int generation)
    {
        lock (_gate)
        {
            if (_disposed || _pauses > 0 || generation != item.Generation || !_items.Remove(item)) return;
            Stop(item);
        }
        Changed?.Invoke();
    }
    /// <summary>Cancel every timer and release subscribers; no notification is persisted.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var item in _items) Stop(item);
            _items.Clear(); Changed = null;
        }
    }
}
