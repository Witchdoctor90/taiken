using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.Postgresql;

namespace Taiken.Messaging;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Wires Wolverine's durable Postgres message storage (ADR 0004): outbox/inbox/dead-letter
    /// tables in the <c>wolverine</c> schema, a <c>CorrelationId</c>/<c>VenueId</c>/message-type
    /// logging scope and structured failure logging on every message, venue-context middleware
    /// for <see cref="IVenueScopedMessage"/>, and a retry policy where transient failures back off
    /// and everything else goes straight to the dead letter queue. Assumes
    /// <c>IVenueContext</c> is already registered (<c>Taiken.Persistence.ServiceCollectionExtensions.AddVenueContext</c>).
    /// </summary>
    public static WolverineOptions AddTaikenMessaging(this WolverineOptions opts, string connectionString)
    {
        opts.PersistMessagesWithPostgresql(connectionString, "wolverine");

        opts.Policies.AddMiddleware(typeof(LoggingScopeMiddleware));
        opts.Policies.AddMiddleware(typeof(MessageLoggingMiddleware));
        opts.Policies.ForMessagesOfType<IVenueScopedMessage>().AddMiddleware(typeof(VenueContextMiddleware));

        // Transient: database/network hiccups — retry with backoff. Everything else: no retry,
        // straight to the dead letter queue (ADR 0004).
        opts.Policies.OnException<NpgsqlException>()
            .Or<TimeoutException>()
            .Or<HttpRequestException>()
            .RetryWithCooldown(TimeSpan.FromMilliseconds(250), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));

        opts.Policies.OnException<Exception>().MoveToErrorQueue();

        return opts;
    }

    /// <summary>
    /// <see cref="AddTaikenMessaging(WolverineOptions, string)"/> plus EF Core transactional
    /// outbox integration for <typeparamref name="TDbContext"/>: application writes and outgoing
    /// messages share one transaction. A module calls this (once, for its own DbContext) when it
    /// registers itself.
    /// </summary>
    public static WolverineOptions AddTaikenMessaging<TDbContext>(this WolverineOptions opts, string connectionString)
        where TDbContext : DbContext
    {
        opts.AddTaikenMessaging(connectionString);
        opts.Services.AddDbContextWithWolverineIntegration<TDbContext>(x => x.UseNpgsql(connectionString));
        opts.UseEntityFrameworkCoreTransactions();
        opts.Policies.AutoApplyTransactions();
        return opts;
    }
}
