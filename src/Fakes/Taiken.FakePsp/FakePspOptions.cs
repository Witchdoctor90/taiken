namespace Taiken.FakePsp;

internal sealed class FakePspOptions
{
    public const string SectionName = "FakePsp";

    public int DefaultTtlSeconds { get; set; } = 900;

    public string WebhookSecret { get; set; } = "fake-psp-dev-secret";

    public TimeSpan WebhookDelay { get; set; } = TimeSpan.FromSeconds(1);
}
