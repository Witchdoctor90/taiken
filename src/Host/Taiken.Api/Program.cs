using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Taiken.Messaging;
using Taiken.Persistence;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException(
        "Missing connection string 'Postgres'. Set it via `dotnet user-secrets set ConnectionStrings:Postgres \"...\"`.");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddVenueContext();

builder.Host.UseWolverine(opts => opts.AddTaikenMessaging(connectionString));

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: ["ready"]);

builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);

var app = builder.Build();

// CorrelationId = the W3C TraceId of the current request's Activity, in the log scope of every
// request (ADR 0004: Observability). VenueId joins it once HTTP-side venue resolution
// (staff claims, guest QR) exists — that's a later task, not TAI-11.
app.Use(async (context, next) =>
{
    using (app.Logger.BeginScope(new Dictionary<string, object?>
    {
        ["CorrelationId"] = Activity.Current?.TraceId.ToString(),
    }))
    {
        await next(context);
    }
});

// Always healthy: process is up and able to serve requests. No tag filter — every
// registered check would have to be "live" too, but none are, so this stays empty on purpose.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
});

// Filtered by the "ready" tag — currently just Postgres.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

if (app.Environment.IsDevelopment())
{
    app.MapTaikenDeadLetterEndpoints();
}

app.Run();
