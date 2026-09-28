using System.Collections.Concurrent;

namespace Taiken.Messaging.Tests;

/// <summary>Shared per-host state so the test can see which cascaded <see cref="NoteRecorded"/> events were delivered.</summary>
public sealed class RecordedNotesState
{
    private readonly ConcurrentQueue<NoteRecorded> _events = new();

    public IReadOnlyCollection<NoteRecorded> Events => _events;

    public void Record(NoteRecorded recorded) => _events.Enqueue(recorded);
}

// Gives RecordNoteHandler's cascaded NoteRecorded event somewhere to route to — without a
// subscriber, Wolverine can't route it and the whole RecordNoteCommand handling fails.
public static class NoteRecordedHandler
{
    public static void Handle(NoteRecorded recorded, RecordedNotesState state) => state.Record(recorded);
}
