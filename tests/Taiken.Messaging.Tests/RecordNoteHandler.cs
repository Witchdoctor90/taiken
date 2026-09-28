using Taiken.Shared;
using Wolverine;

namespace Taiken.Messaging.Tests;

// Canonical example (ADR 0004: "перший обробник, написаний у TAI-11, стає еталоном"). It shows
// the whole pattern a module handler follows: a venue-scoped command, a DbContext parameter that
// Wolverine's EF Core transactional middleware wraps in the same transaction as the outbox, and a
// cascaded event published from inside that transaction (delivered only after the commit).

public sealed record RecordNoteCommand(VenueId VenueId, string Text) : IVenueScopedMessage;

public sealed record NoteRecorded(VenueId VenueId, string Text);

public static class RecordNoteHandler
{
    public static NoteRecorded Handle(RecordNoteCommand command, MessagingTestDbContext db)
    {
        db.Notes.Add(new Note { Id = Guid.NewGuid(), VenueId = command.VenueId, Text = command.Text });
        return new NoteRecorded(command.VenueId, command.Text);
    }
}
