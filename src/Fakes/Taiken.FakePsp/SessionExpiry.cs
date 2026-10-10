namespace Taiken.FakePsp;

internal static class SessionExpiry
{
    /// <summary>
    /// Lazily resolves <see cref="SessionStatus.Expired"/>: the "Таймаут" scenario sends no
    /// webhook at all, so the only way a session ever becomes <c>EXP</c> is this check running on
    /// the next read — same as a real reconciler noticing nothing arrived.
    /// </summary>
    public static SessionStatus EffectiveStatus(Session session, TimeProvider timeProvider)
    {
        if (session.Status == SessionStatus.Pending && timeProvider.GetUtcNow() >= session.CreatedAt.AddSeconds(session.Ttl))
        {
            session.Status = SessionStatus.Expired;
        }

        return session.Status;
    }
}
