using Microsoft.AspNetCore.Mvc;
using vehicle_scheduling_be.Services;

namespace vehicle_scheduling_be.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehicleSchedulerController : ControllerBase
{
    private readonly IVehicleSchedulerService _schedulerService;

    public VehicleSchedulerController(IVehicleSchedulerService schedulerService)
    {
        _schedulerService = schedulerService;
    }

    [HttpGet("schedule")]
    public async Task<IActionResult> Schedule([FromHeader(Name = "Authorization")] string? authorization)
    {
        if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer "))
        {
            return Unauthorized(new { error = "Bearer token required in Authorization header" });
        }

        var token = authorization.Substring("Bearer ".Length).Trim();

        try
        {
            var result = await _schedulerService.ScheduleAsync(token);
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new { error = "Failed to reach evaluation service", detail = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal error", detail = ex.Message });
        }
    }
}