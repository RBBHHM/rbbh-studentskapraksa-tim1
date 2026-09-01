using System.Collections.Concurrent;

namespace RBBH.ConnectedParties.BL.Services;

public sealed class EmailLogEntry
{
    public Guid     Id       { get; init; } = Guid.NewGuid();
    public string   To       { get; init; } = "";
    public string   Subject  { get; init; } = "";
    public string   HtmlBody { get; init; } = "";
    public DateTime SentAt   { get; init; } = DateTime.UtcNow;
    /// <summary>"admin" — notifikacija za admina | "user" — notifikacija za korisnika</summary>
    public string Audience  { get; init; } = "admin";
    /// <summary>ID zahtjeva na koji se odnosi email (postavljeno samo za NeedsInfo emailove).</summary>
    public Guid?  RequestId { get; init; }
}

/// <summary>
/// Singleton in-memory store za demo mod. Čuva posljednja 50 "emailova".
/// U SMTP modu ova klasa ostaje registrovana ali se ne koristi.
/// </summary>
public sealed class EmailLogStore
{
    private readonly ConcurrentQueue<EmailLogEntry> _entries = new();
    private const int Max = 50;

    public void Add(EmailLogEntry entry)
    {
        _entries.Enqueue(entry);
        while (_entries.Count > Max)
            _entries.TryDequeue(out _);
    }

    public IReadOnlyList<EmailLogEntry> GetAll() =>
        [.. _entries.OrderByDescending(e => e.SentAt)];

    public IReadOnlyList<EmailLogEntry> GetForAudience(string audience) =>
        [.. _entries.Where(e => e.Audience == audience).OrderByDescending(e => e.SentAt)];
}
