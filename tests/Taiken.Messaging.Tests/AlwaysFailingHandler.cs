namespace Taiken.Messaging.Tests;

/// <summary>Shared per-host state so the test can see how many times the handler actually ran.</summary>
public sealed class AlwaysFailingState
{
    private int _attempts;

    public int Attempts => _attempts;

    public bool ShouldSucceed;

    public void RecordAttempt() => Interlocked.Increment(ref _attempts);
}

public sealed record AlwaysFailingCommand(Guid Id);

public static class AlwaysFailingHandler
{
    public static void Handle(AlwaysFailingCommand command, AlwaysFailingState state)
    {
        state.RecordAttempt();

        if (!state.ShouldSucceed)
        {
            // A timeout is one of the "transient" exception types AddTaikenMessaging retries with
            // backoff before giving up (ADR 0004) — that exhaustion is what lands this in the dead
            // letter queue, matching TAI-11's acceptance test: "fails N times, lands in dead letter".
            throw new TimeoutException("Simulated transient failure.");
        }
    }
}
