using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Taiken.Persistence;
using Taiken.Shared;
using Wolverine;
using Wolverine.Persistence.Durability;
using Wolverine.Persistence.Durability.DeadLetterManagement;
using Wolverine.Util;

namespace Taiken.Messaging.Tests;

// The dead-letter test is the one ADR 0004 asks TAI-11 for: "обробник, що падає N разів,
// потрапляє в dead letter; повтор після виправлення обробляє його один раз."
[Collection(PostgreSqlCollection.Name)]
public sealed class MessagingTests(MessagingFixture fixture)
{
    [Fact]
    public async Task Outbox_write_is_atomic_with_the_database_write()
    {
        var venueId = VenueId.New();
        using var host = await BuildHostAsync();

        await host.Services.GetRequiredService<IMessageBus>().InvokeAsync(new RecordNoteCommand(venueId, "hello"));

        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MessagingTestDbContext>();
        var saved = await db.Notes.SingleAsync(n => n.VenueId == venueId);

        Assert.Equal("hello", saved.Text);

        // The cascaded NoteRecorded event is only published after the transaction commits (ADR 0004).
        var notesState = host.Services.GetRequiredService<RecordedNotesState>();
        var recorded = await WaitUntilAsync(() =>
            Task.FromResult(notesState.Events.FirstOrDefault(e => e.VenueId == venueId)));
        Assert.Equal("hello", recorded.Text);
    }

    [Fact]
    public async Task Handler_that_keeps_failing_is_dead_lettered_then_processed_once_on_replay()
    {
        using var host = await BuildHostAsync();
        var state = host.Services.GetRequiredService<AlwaysFailingState>();
        var store = host.Services.GetRequiredService<IMessageStore>();
        var commandId = Guid.NewGuid();

        await host.Services.GetRequiredService<IMessageBus>().PublishAsync(new AlwaysFailingCommand(commandId));

        // DeadLetterEnvelope.Message is not rehydrated by QueryAsync (it stays null) — the query
        // reads only the stored columns, not the serialized body — so matching has to go through
        // MessageType rather than the deserialized message. This test dead-letters exactly one
        // command, so MessageType alone is unambiguous.
        var deadLetter = await WaitUntilAsync(async () =>
        {
            var results = await store.DeadLetters.QueryAsync(new DeadLetterEnvelopeQuery(), CancellationToken.None);
            return results.Envelopes.FirstOrDefault(e => e.MessageType == typeof(AlwaysFailingCommand).ToMessageTypeName());
        });

        var attemptsBeforeReplay = state.Attempts;
        Assert.True(attemptsBeforeReplay > 1, "expected retries before the message was dead-lettered");

        state.ShouldSucceed = true;
        await store.DeadLetters.ReplayAsync(new DeadLetterEnvelopeQuery([deadLetter.Id]), CancellationToken.None);

        await WaitUntilAsync(() =>
            Task.FromResult(state.Attempts > attemptsBeforeReplay ? (int?)state.Attempts : null));

        // Replay hands it to the handler exactly once more — not re-retried into another N attempts.
        Assert.Equal(attemptsBeforeReplay + 1, state.Attempts);
    }

    private async Task<IHost> BuildHostAsync()
    {
        return await Host.CreateDefaultBuilder()
            .UseWolverine(opts => opts.AddTaikenMessaging<MessagingTestDbContext>(fixture.ConnectionString))
            .ConfigureServices(services =>
            {
                services.AddVenueContext();
                services.AddSingleton<AlwaysFailingState>();
                services.AddSingleton<RecordedNotesState>();
            })
            .StartAsync();
    }

    private static async Task<T> WaitUntilAsync<T>(Func<Task<T?>> probe, int timeoutSeconds = 30)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var result = await probe();
            if (result is not null)
            {
                return result;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        throw new TimeoutException("Condition was not met in time.");
    }
}
