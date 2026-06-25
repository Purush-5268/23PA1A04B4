using System.Text.Json.Serialization;

namespace CampusNotifications.Models;

public class Notification
{
    public string ID { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class NotificationResponse
{
    [JsonPropertyName("notifications")]
    public List<Notification> Notifications { get; set; } = new();
}

public class PriorityNotification : Notification
{
    public int PriorityScore { get; set; }
}