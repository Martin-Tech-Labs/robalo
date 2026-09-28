
using Robalo.Common.Health;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables("ROBALO_");

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSerilog();
builder.Services.AddHealthChecks()
    .AddCheck<TestHealthCheck>("Test");


var app = builder.Build();

// _System endpoints - Ping, Health, Metrics, etc.
app.MapGet("/_system/ping", () => Results.Ok("pong"));
app.MapHealthChecks("/_system/health");


try
{
    Log.Information("Starting Robalo.Controller.Api");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    throw;
}
finally
{
    Log.Information("Shutting down Robalo.Controller.Api");
    Log.CloseAndFlush();
}


record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
