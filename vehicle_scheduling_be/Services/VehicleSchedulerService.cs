using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using vehicle_scheduling_be.Models;

namespace vehicle_scheduling_be.Services;

public interface IVehicleSchedulerService
{
    Task<SchedulerResponse> ScheduleAsync(string authToken);
}

public class VehicleSchedulerService : IVehicleSchedulerService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://4.224.186.213/evaluation-service";

    public VehicleSchedulerService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Helper method to satisfy the strict remote logging rule
    private async Task RemoteLogAsync(string authToken, string level, string package, string message)
    {
        var logPayload = new { stack = "backend", level = level.ToLower(), package = package.ToLower(), message = message };
        var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/logs");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        request.Content = new StringContent(JsonSerializer.Serialize(logPayload), Encoding.UTF8, "application/json");
        try { await _httpClient.SendAsync(request); } catch { /* Ignore local failures */ }
    }

    public async Task<SchedulerResponse> ScheduleAsync(string authToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

        var depotsJson = await _httpClient.GetStringAsync($"{BaseUrl}/depots");
        var depotsData = JsonSerializer.Deserialize<DepotResponse>(depotsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var vehiclesJson = await _httpClient.GetStringAsync($"{BaseUrl}/vehicles");
        var vehiclesData = JsonSerializer.Deserialize<VehicleResponse>(vehiclesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var vehicles = vehiclesData?.Vehicles ?? new List<Vehicle>();
        var depots = depotsData?.Depots ?? new List<Depot>();

        var response = new SchedulerResponse();

        foreach (var depot in depots)
        {
            // USING THE CUSTOM REMOTE LOGGER INSTEAD OF ILOGGER
            await RemoteLogAsync(authToken, "info", "service", $"Scheduling for Depot {depot.ID} with {depot.MechanicHours} mechanic hours");
            var result = SolveKnapsack(vehicles, depot);
            response.Results.Add(result);
        }

        return response;
    }

    private ScheduleResult SolveKnapsack(List<Vehicle> vehicles, Depot depot)
    {
        int n = vehicles.Count, W = depot.MechanicHours;
        var dp = new int[n + 1, W + 1];

        for (int i = 1; i <= n; i++)
        {
            var v = vehicles[i - 1];
            for (int w = 0; w <= W; w++)
            {
                dp[i, w] = dp[i - 1, w];
                if (v.Duration <= w)
                    dp[i, w] = Math.Max(dp[i, w], dp[i-1, w-v.Duration] + v.Impact);
            }
        }

        var selected = new List<Vehicle>();
        int remaining = W;
        for (int i = n; i >= 1; i--)
            if (dp[i, remaining] != dp[i-1, remaining]) {
                selected.Add(vehicles[i-1]);
                remaining -= vehicles[i-1].Duration;
            }

        return new ScheduleResult {
            DepotID = depot.ID,
            MechanicHoursAvailable = depot.MechanicHours,
            TotalHoursUsed = selected.Sum(v => v.Duration),
            TotalImpactScore = selected.Sum(v => v.Impact),
            SelectedTasks = selected
        };
    }
}