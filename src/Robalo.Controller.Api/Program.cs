using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Metrics;
using Robalo.Common.Health;
using Robalo.Common.Models;
using Robalo.Common.Services;
using Robalo.Controller.Api;
using Robalo.Controller.Api.Filters;
using Robalo.Controller.Api.Requests;
using Robalo.Controller.Api.Validation;
using Robalo.Controller.Api.Validation.Requests;
using Robalo.Domain.Repositories;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;

var startTimeUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables("ROBALO_");

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.WithProperty("Version", Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.ToString() ?? string.Empty)
    .CreateLogger();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSerilog(Log.Logger);
builder.Services.AddHealthChecks()
    .AddCheck<TestHealthCheck>("Test One")
    .AddCheck<AnotherHealthCheck>("Test Another");

builder.Services.AddOpenTelemetry()
    .WithMetrics(builder =>
    {
        builder.AddPrometheusExporter();
        builder.AddOtlpExporter();

        builder.AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel");
        builder.AddView("http.server.request.duration",
           new ExplicitBucketHistogramConfiguration
           {
               Boundaries = [ 0, 0.005, 0.01, 0.025, 0.05,
                       0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 ]
           });
    });

builder.Services.AddSingleton<IThreadRepository, InMemoryThreadRepository>();
builder.Services.AddTransient<IDateTimeOffsetProvider, DateTimeOffsetProvider>();
builder.Services.AddFluentValidationAutoValidation(configuration =>
{
    configuration.DisableBuiltInModelValidation = true;
    configuration.OverrideDefaultResultFactoryWith<ResultFactory>();
});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions.Remove("traceId");
        context.ProblemDetails.Extensions["request_id"] =
            context.HttpContext.TraceIdentifier;
    };
});

builder.Services
    .AddControllers(configure => configure.Filters.Add<InvalidBindingFilter>(int.MinValue))
    .AddJsonOptions(options =>
    {
        // options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddScoped<IValidator<CreateThreadApiRequest>, CreateThreadApiRequestValidator>();

var app = builder.Build();
app.UseSerilogRequestLogging();



app.MapControllers();

app.MapPrometheusScrapingEndpoint("/_system/metrics");


// _System endpoints - Ping, Health, Metrics, etc.
app.MapGet("/_system/ping", () => Results.Text("pong"));
app.MapHealthChecks("/_system/health", new()
{
    ResponseWriter = static (context, report) =>
        context.Response.WriteAsJsonAsync(
            report.Entries.ToDictionary(e => e.Key, e => e.Value.Description),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = true
            })
});

app.MapGet("/_system/env", (IHostEnvironment hostEnvironment) =>
{
    var environmentInfo = new EnvironmentInfo(
        ApplicationName: hostEnvironment?.ApplicationName,
        Version: Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
        OS: $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})",
        Machine: Environment.MachineName,
        Environment: hostEnvironment?.EnvironmentName,
        Runtime: RuntimeInformation.FrameworkDescription,
        RunningInContainer: Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")?.Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? false,
        UptimeSeconds: (long)(DateTime.UtcNow - startTimeUtc).TotalSeconds);

    return Results.Json(environmentInfo, new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    });
});
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
