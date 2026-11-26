using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.Entities.Notifications;

public sealed class ApplicationNotification
{
    public long Id { get; set; }
    [MaxLength(100)] public string RecipientSubject { get; set; } = "";
    [MaxLength(200)] public string RecipientEmail { get; set; } = "";
    [MaxLength(200)] public string EventKey { get; set; } = "";
    [MaxLength(200)] public string TitleKey { get; set; } = "";
    [MaxLength(200)] public string BodyKey { get; set; } = "";
    [MaxLength(32000)] public string ParametersJson { get; set; } = "{}";
    [MaxLength(200)] public string Route { get; set; } = "/app/notifications";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
    public NotificationDelivery? Delivery { get; set; }
}

public sealed class NotificationDelivery
{
    public long Id { get; set; }
    public long NotificationId { get; set; }
    public ApplicationNotification Notification { get; set; } = null!;
    public int Attempts { get; set; }
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;
    public Guid? LeaseId { get; set; }
    public DateTime? LeaseUntil { get; set; }
    public DateTime? DeliveredAt { get; set; }
    [MaxLength(100)] public string? FailureCode { get; set; }
}
