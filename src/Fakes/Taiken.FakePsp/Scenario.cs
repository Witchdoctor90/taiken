using System.Globalization;

namespace Taiken.FakePsp;

internal static class Scenario
{
    public static readonly string[] Known =
        ["success", "decline", "timeout", "success-duplicate-webhook", "success-different-amount"];

    /// <summary>Applies one HPP button's scenario to the session; returns how many webhook deliveries to fire.</summary>
    public static int Apply(Session session, string scenario) => scenario switch
    {
        "success" => Approve(session, session.AmountMajor),
        "decline" => Decline(session),
        "timeout" => 0, // deliberately silent — only the reconciler (TAI-17) ever notices this one
        "success-duplicate-webhook" => ApproveWithDuplicateWebhook(session),
        "success-different-amount" => Approve(session, DifferentAmount(session.AmountMajor)),
        _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown scenario."),
    };

    private static int Approve(Session session, string paid)
    {
        session.Status = SessionStatus.Approved;
        session.Paid = paid;
        return 1;
    }

    private static int Decline(Session session)
    {
        session.Status = SessionStatus.Declined;
        session.Paid = "0.00";
        return 1;
    }

    private static int ApproveWithDuplicateWebhook(Session session)
    {
        Approve(session, session.AmountMajor);
        return 2;
    }

    private static string DifferentAmount(string amountMajor)
    {
        var amount = decimal.Parse(amountMajor, CultureInfo.InvariantCulture);
        var different = Math.Max(0, amount - 1.00m);
        return different.ToString("F2", CultureInfo.InvariantCulture);
    }
}
