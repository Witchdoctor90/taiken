using Taiken.Shared;

namespace Taiken.Persistence;

/// <summary>
/// The venue (or system-wide) scope of the current request, message, or background iteration.
/// Fail closed: accessing <see cref="VenueId"/> before a scope is set, or while in system mode,
/// throws rather than returning an empty or "all venues" result (ADR 0002).
/// </summary>
public interface IVenueContext
{
    bool IsSystem { get; }

    VenueId VenueId { get; }

    /// <summary>
    /// Same as <see cref="VenueId"/>, but returns <c>null</c> in system mode instead of throwing —
    /// for the query filter, where "system" already means "see every venue" via <see cref="IsSystem"/>
    /// and evaluating <see cref="VenueId"/> unconditionally would defeat that. Still throws if no
    /// scope has been set at all (fail closed, ADR 0002). Infrastructure use only; domain code should
    /// use <see cref="VenueId"/>.
    /// </summary>
    VenueId? VenueIdOrNull { get; }

    void Enter(VenueId venueId);

    void EnterSystem();
}
