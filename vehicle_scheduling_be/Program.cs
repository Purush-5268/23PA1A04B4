// using System.Net.Http.Headers;
// using System.Text;
// using System.Text.Json;

// var builder = WebApplication.CreateBuilder(args);

// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();
// builder.Services.AddHttpClient();

// var app = builder.Build();

// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

// app.UseHttpsRedirection();

// async Task RemoteLog(HttpClient client, string level, string package, string message)
// {
//     var logPayload = new
//     {
//         stack = "backend",
//         level = level.ToLower(),
//         package = package.ToLower(),
//         message = message
//     };

//     var request = new HttpRequestMessage(HttpMethod.Post, "http://4.224.186.213/evaluation-service/logs");
    
//     // Injecting your live access token safely
//     request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJNYXBDbGFpbXMiOnsiYXVkIjoiaHR0cDovLzIwLjI0NC41Ni4xNDQvZXZhbHVhdGlvbi1zZXJ2aWNlIiwiZW1haWwiOiJwYWlkaXBpbGxpcHVydXNob3RoYW1AZ21haWwuY29tIiwiZXhwIjoxNzgyMzc4MzIyLCJpYXQiOjE3ODIzNzc0MjIsImlzcyI6IkFmZm9yZCBNZWRpY2FsIFRlY2hub2xvZ2llcyBQcml2YXRlIExpbWl0ZWQiLCJqdGkiOiJmYmRjOWU3Ny02NmE1LTRjODItYmY5Ni05NTAxNzFmYjM0OWYiLCJsb2NhbGUiOiJlbi1JTiIsIm5hbWUiOiJwYWlkaXBpbGxpIHB1cnVzaG90aGFtIiwic3ViIjoiYTM4MTZmY2QtNWIzNC00YzFhLTkyMDYtNzI2MmRlZTVkZDJiIn0sImVtYWlsIjoicGFpZGlwaWxsaXB1cnVzaG90aGFtQGdtYWlsLmNvbSIsIm5hbWUiOiJwYWlkaXBpbGxpIHB1cnVzaG90aGFtIiwicm9sbE5vIjoiMjNwYTFhMDRiNCIsImFjY2Vzc0NvZGUiOiJhaFhqdnAiLCJjbGllbnRJRCI6ImEzODE2ZmNkLTViMzQtNGMxYS05MjA2LTcyNjJkZWU1ZGQyYiIsImNsaWVudFNlY3JldCI6ImtoeFJmbUFwcEFRSHh6eXQifQ.cNJJoxTnYCcf2hFRucOs_95Jvv-8_xGOkbTiXW5b0zY");
    
//     request.Content = new StringContent(
//         JsonSerializer.Serialize(logPayload), 
//         Encoding.UTF8, 
//         "application/json"
//     );

//     try
//     {
//         await client.SendAsync(request);
//     }
//     catch (Exception ex)
//     {
//         Console.WriteLine($"Local console fallback log: {message} (Remote log failed: {ex.Message})");
//     }
// }



// app.MapGet("/test-log", async (IHttpClientFactory clientFactory) =>
// {
//     var client = clientFactory.CreateClient();
//     await RemoteLog(client, "info", "middleware", "Prerequisites verified. Handshake success.");
//     return Results.Ok(new { message = "Log transmission successful!" });
// });

// app.Run();
using Microsoft.AspNetCore.Mvc;
using vehicle_scheduling_be.Middleware;
using vehicle_scheduling_be.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddHttpClient<IVehicleSchedulerService, VehicleSchedulerService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// MANDATORY: Register your custom remote logging middleware
app.UseMiddleware<LoggingMiddleware>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map the VehicleSchedulerController
app.MapControllers();

app.Run();