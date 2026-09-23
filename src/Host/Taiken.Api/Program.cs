var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Always healthy: process is up and able to serve requests.
app.MapGet("/health/live", () => Results.Ok());

// No dependency checks yet — Postgres is added in M2.
app.MapGet("/health/ready", () => Results.Ok());

app.Run();
