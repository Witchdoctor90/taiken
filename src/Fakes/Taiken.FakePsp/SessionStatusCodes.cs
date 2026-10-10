namespace Taiken.FakePsp;

/// <summary>The fake's deliberately "inconvenient" status codes — short, provider-specific, nothing like the canonical <c>ProviderPaymentStatus</c> an adapter maps these into.</summary>
internal static class SessionStatusCodes
{
    public static string ToCode(SessionStatus status) => status switch
    {
        SessionStatus.Pending => "PND",
        SessionStatus.Approved => "APR",
        SessionStatus.Declined => "DCL",
        SessionStatus.Expired => "EXP",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown session status."),
    };
}
