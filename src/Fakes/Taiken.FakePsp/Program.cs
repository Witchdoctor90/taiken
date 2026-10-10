using Taiken.FakePsp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<SessionStore>();
builder.Services.AddSingleton<WebhookSender>();
builder.Services.Configure<FakePspOptions>(builder.Configuration.GetSection(FakePspOptions.SectionName));
builder.Services.AddHttpClient(WebhookSender.HttpClientName);

var app = builder.Build();

app.MapPost("/v2/sessions", SessionEndpoints.CreateSession);
app.MapGet("/v2/sessions/{sid}", SessionEndpoints.GetSession);
app.MapGet("/pay/{sid}", PayEndpoints.GetPayPage);
app.MapPost("/pay/{sid}/scenario/{scenario}", PayEndpoints.ApplyScenario);

app.Run();

// WebApplicationFactory<Program> (Taiken.FakePsp.Tests) needs this type to be visible.
public partial class Program;
