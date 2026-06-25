using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CampusNotifications.Models;

namespace CampusNotifications.Services;

public interface INotificationService
{
    Task<List<PriorityNotification>> GetTopPriorityNotificationsAsync(string authToken, int topN);
}

public class NotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://4.224.186.213/evaluation-service/notifications";
    private const string LogUrl = "http://4.224.186.213/evaluation-service/logs";

    public NotificationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private async Task RemoteLogAsync(string authToken, string level, string package, string message)
    {
        var logPayload = new { stack = "backend", level = level.ToLower(), package = package.ToLower(), message = message };
        var request = new HttpRequestMessage(HttpMethod.Post, LogUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        request.Content = new StringContent(JsonSerializer.Serialize(logPayload), Encoding.UTF8, "application/json");
        try { await _httpClient.SendAsync(request); } catch { }
    }

    public async Task<List<PriorityNotification>> GetTopPriorityNotificationsAsync(string authToken, int topN)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

        var json = await _httpClient.GetStringAsync(BaseUrl);
        var data = JsonSerializer.Deserialize<NotificationResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        var notifications = data?.Notifications ?? new List<Notification>();

        // Remote Logging instead of ILogger
        await RemoteLogAsync(authToken, "info", "service", $"Computing priority inbox (top {topN}) from {notifications.Count} notifications");

        var priorityList = notifications.Select(n => new PriorityNotification
        {
            ID = n.ID,
            Type = n.Type,
            Message = n.Message,
            Timestamp = n.Timestamp,
            PriorityScore = CalculatePriority(n.Type)
        })
        .OrderByDescending(n => n.PriorityScore)
        .ThenByDescending(n => n.Timestamp)
        .Take(topN)
        .ToList();

        return priorityList;
    }

    private int CalculatePriority(string type)
    {
        return type.ToLower() switch
        {
            "placement" => 3,
            "result" => 2,
            "event" => 1,
            _ => 0
        };
    }
}