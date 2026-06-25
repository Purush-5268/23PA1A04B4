using Microsoft.AspNetCore.Mvc;
using CampusNotifications.Services;

namespace CampusNotifications.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet("priority-inbox")]
    public async Task<IActionResult> PriorityInbox(
        [FromHeader(Name = "Authorization")] string? authorization,
        [FromQuery] int topN = 10)
    {
        if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer "))
        {
            return Unauthorized(new { error = "Bearer token required in Authorization header" });
        }

        var token = authorization.Substring("Bearer ".Length).Trim();

        if (topN < 1 || topN > 100)
        {
            return BadRequest(new { error = "topN must be between 1 and 100" });
        }

        try
        {
            var top = await _notificationService.GetTopPriorityNotificationsAsync(token, topN);
            return Ok(new { topN, notifications = top });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new { error = "Failed to reach notification service", detail = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal error", detail = ex.Message });
        }
    }
}