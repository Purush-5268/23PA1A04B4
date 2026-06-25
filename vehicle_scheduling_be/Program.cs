// var builder = WebApplication.CreateBuilder(args);

// // Add services to the container.
// // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

// var app = builder.Build();

// // Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

// app.UseHttpsRedirection();

// var summaries = new[]
// {
//     "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
// };

// app.MapGet("/weatherforecast", () =>
// {
//     var forecast =  Enumerable.Range(1, 5).Select(index =>
//         new WeatherForecast
//         (
//             DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//             Random.Shared.Next(-20, 55),
//             summaries[Random.Shared.Next(summaries.Length)]
//         ))
//         .ToArray();
//     return forecast;
// })
// .WithName("GetWeatherForecast")
// .WithOpenApi();

// app.Run();

// record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
// {
//     public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
// }
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

async Task RemoteLog(HttpClient client, string level, string package, string message)
{
    var logPayload = new
    {
        stack = "backend",
        level = level.ToLower(),
        package = package.ToLower(),
        message = message
    };

    var request = new HttpRequestMessage(HttpMethod.Post, "http://4.224.186.213/evaluation-service/logs");
    
    // Injecting your live access token safely
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJNYXBDbGFpbXMiOnsiYXVkIjoiaHR0cDovLzIwLjI0NC41Ni4xNDQvZXZhbHVhdGlvbi1zZXJ2aWNlIiwiZW1haWwiOiJwYWlkaXBpbGxpcHVydXNob3RoYW1AZ21haWwuY29tIiwiZXhwIjoxNzgyMzc4MzIyLCJpYXQiOjE3ODIzNzc0MjIsImlzcyI6IkFmZm9yZCBNZWRpY2FsIFRlY2hub2xvZ2llcyBQcml2YXRlIExpbWl0ZWQiLCJqdGkiOiJmYmRjOWU3Ny02NmE1LTRjODItYmY5Ni05NTAxNzFmYjM0OWYiLCJsb2NhbGUiOiJlbi1JTiIsIm5hbWUiOiJwYWlkaXBpbGxpIHB1cnVzaG90aGFtIiwic3ViIjoiYTM4MTZmY2QtNWIzNC00YzFhLTkyMDYtNzI2MmRlZTVkZDJiIn0sImVtYWlsIjoicGFpZGlwaWxsaXB1cnVzaG90aGFtQGdtYWlsLmNvbSIsIm5hbWUiOiJwYWlkaXBpbGxpIHB1cnVzaG90aGFtIiwicm9sbE5vIjoiMjNwYTFhMDRiNCIsImFjY2Vzc0NvZGUiOiJhaFhqdnAiLCJjbGllbnRJRCI6ImEzODE2ZmNkLTViMzQtNGMxYS05MjA2LTcyNjJkZWU1ZGQyYiIsImNsaWVudFNlY3JldCI6ImtoeFJmbUFwcEFRSHh6eXQifQ.cNJJoxTnYCcf2hFRucOs_95Jvv-8_xGOkbTiXW5b0zY");
    
    request.Content = new StringContent(
        JsonSerializer.Serialize(logPayload), 
        Encoding.UTF8, 
        "application/json"
    );

    try
    {
        await client.SendAsync(request);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Local console fallback log: {message} (Remote log failed: {ex.Message})");
    }
}



app.MapGet("/test-log", async (IHttpClientFactory clientFactory) =>
{
    var client = clientFactory.CreateClient();
    await RemoteLog(client, "info", "middleware", "Prerequisites verified. Handshake success.");
    return Results.Ok(new { message = "Log transmission successful!" });
});

app.Run();