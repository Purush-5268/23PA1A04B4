using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace vehicle_scheduling_be.Middleware;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HttpClient _httpClient;

    public LoggingMiddleware(RequestDelegate next)
    {
        _next = next;
        _httpClient = new HttpClient();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Log the incoming request
        await RemoteLog("info", "middleware", $"[REQUEST] {context.Request.Method} {context.Request.Path}");

        await _next(context);

        // 2. Log the outgoing response
        await RemoteLog("info", "middleware", $"[RESPONSE] {context.Request.Method} {context.Request.Path} => {context.Response.StatusCode}");
    }

    private async Task RemoteLog(string level, string package, string message)
    {
        var logPayload = new
        {
            stack = "backend",
            level = level.ToLower(),
            package = package.ToLower(),
            message = message
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "http://4.224.186.213/evaluation-service/logs");
        
        // Your live token
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJNYXBDbGFpbXMiOnsiYXVkIjoiaHR0cDovLzIwLjI0NC41Ni4xNDQvZXZhbHVhdGlvbi1zZXJ2aWNlIiwiZW1haWwiOiJwYWlkaXBpbGxpcHVydXNob3RoYW1AZ21haWwuY29tIiwiZXhwIjoxNzgyMzc4MzIyLCJpYXQiOjE3ODIzNzc0MjIsImlzcyI6IkFmZm9yZCBNZWRpY2FsIFRlY2hub2xvZ2llcyBQcml2YXRlIExpbWl0ZWQiLCJqdGkiOiJmYmRjOWU3Ny02NmE1LTRjODItYmY5Ni05NTAxNzFmYjM0OWYiLCJsb2NhbGUiOiJlbi1JTiIsIm5hbWUiOiJwYWlkaXBpbGxpIHB1cnVzaG90aGFtIiwic3ViIjoiYTM4MTZmY2QtNWIzNC00YzFhLTkyMDYtNzI2MmRlZTVkZDJiIn0sImVtYWlsIjoicGFpZGlwaWxsaXB1cnVzaG90aGFtQGdtYWlsLmNvbSIsIm5hbWUiOiJwYWlkaXBpbGxpIHB1cnVzaG90aGFtIiwicm9sbE5vIjoiMjNwYTFhMDRiNCIsImFjY2Vzc0NvZGUiOiJhaFhqdnAiLCJjbGllbnRJRCI6ImEzODE2ZmNkLTViMzQtNGMxYS05MjA2LTcyNjJkZWU1ZGQyYiIsImNsaWVudFNlY3JldCI6ImtoeFJmbUFwcEFRSHh6eXQifQ.cNJJoxTnYCcf2hFRucOs_95Jvv-8_xGOkbTiXW5b0zY");
        
        request.Content = new StringContent(JsonSerializer.Serialize(logPayload), Encoding.UTF8, "application/json");

        try { await _httpClient.SendAsync(request); } 
        catch { /* Fails silently locally so it doesn't crash your server */ }
    }
}