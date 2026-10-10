using System.Collections.Concurrent;

namespace Taiken.FakePsp;

internal sealed class SessionStore
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new();
    private readonly ConcurrentDictionary<string, string> _idempotencyKeys = new();
    private int _counter;

    public Session CreateOrGetExisting(string idempotencyKey, Func<string, Session> factory)
    {
        if (_idempotencyKeys.TryGetValue(idempotencyKey, out var existingSid))
        {
            return _sessions[existingSid];
        }

        var sid = $"S-{Interlocked.Increment(ref _counter)}";
        var session = factory(sid);
        _sessions[sid] = session;

        if (_idempotencyKeys.TryAdd(idempotencyKey, sid))
        {
            return session;
        }

        // Lost a race to another request with the same Idempotency-Key — drop ours, use theirs.
        _sessions.TryRemove(sid, out _);
        return _sessions[_idempotencyKeys[idempotencyKey]];
    }

    public Session? Find(string sid) => _sessions.GetValueOrDefault(sid);
}
