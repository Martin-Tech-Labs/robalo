namespace Robalo.Controller.Api;

sealed record EnvironmentInfo(
    string? ApplicationName, 
    string? Version, 
    string? OS, 
    string? Machine, 
    string? Environment,
    string? Runtime,
    bool RunningInContainer,
    long UptimeSeconds);