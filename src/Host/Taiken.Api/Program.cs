using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

// Always healthy: process is up and able to serve requests. No tag filter — every
// registered check would have to be "live" too, but none are, so this stays empty on purpose.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
});

// Filtered by the "ready" tag. No checks carry it yet — Postgres is added in M2 as one line here.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.Run();
