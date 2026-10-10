using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Taiken.FakePsp.Tests;

internal sealed class FakePspFactory : WebApplicationFactory<Program>
{
    public FakeTimeProvider TimeProvider { get; } = new(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

    public CapturingHandler WebhookHandler { get; } = new();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(TimeProvider);

            // No real delay in tests — the delay itself is covered by WebhookSender using
            // TimeProvider.Delay, not by waiting it out here.
            services.Configure<FakePspOptions>(options => options.WebhookDelay = TimeSpan.Zero);

            services.AddHttpClient(WebhookSender.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => WebhookHandler);
        });
    }
}
