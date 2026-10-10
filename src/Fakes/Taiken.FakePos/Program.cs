using Microsoft.EntityFrameworkCore;
using Taiken.FakePos;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("FakePos") ?? "Data Source=fakepos.db";
builder.Services.AddDbContext<FakePosDbContext>(options => options.UseSqlite(connectionString));

// Local dev/test tool only, never deployed as a real public API — the admin emulator (a separate
// Electron/React app) calls this from file:// or a different localhost port, so CORS has to be
// wide open rather than locked to one origin.
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FakePosDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DemoSeed.EnsureSeededAsync(db, CancellationToken.None);
}

app.MapGet("/api/v1/locations/{loc}/tables/{no}/check", PosApiEndpoints.GetCheck);
app.MapPost("/api/v1/checks/{checkId}/payments", PosApiEndpoints.RegisterPayment);

app.MapGet("/admin/api/locations", AdminApiEndpoints.Locations);
app.MapGet("/admin/api/locations/{loc}/tables", AdminApiEndpoints.Tables);
app.MapGet("/admin/api/locations/{loc}/tables/{no}", AdminApiEndpoints.TableDetail);
app.MapPost("/admin/api/locations/{loc}/tables/{no}/items", AdminApiEndpoints.AddItem);
app.MapPost("/admin/api/locations/{loc}/tables/{no}/reset", AdminApiEndpoints.ResetCheck);

app.Run();

// WebApplicationFactory<Program> (Taiken.FakePos.Tests) needs this type to be visible.
public partial class Program;
