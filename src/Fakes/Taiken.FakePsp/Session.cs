namespace Taiken.FakePsp;

internal sealed class Session
{
    public required string Sid { get; init; }

    public required string AmountMajor { get; init; }

    public required string Currency { get; init; }

    public required string Merchant { get; init; }

    public required string OrderRef { get; init; }

    public required string CallbackUrl { get; init; }

    public required string EmbedOrigin { get; init; }

    public required int Ttl { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public SessionStatus Status { get; set; } = SessionStatus.Pending;

    public string Paid { get; set; } = "0.00";
}
