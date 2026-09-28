using Microsoft.AspNetCore.Routing;
using Wolverine.Http;

namespace Taiken.Messaging;

public static class DeadLetterEndpoints
{
    /// <summary>
    /// List, replay, and delete dead-lettered messages (Wolverine's built-in
    /// <c>/dead-letters</c> REST API). The caller decides when this is exposed — dev-only, or
    /// behind auth (ADR 0004: "мінімальний спосіб побачити dead letters і повторити їх").
    /// </summary>
    public static IEndpointRouteBuilder MapTaikenDeadLetterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDeadLettersEndpoints();
        return endpoints;
    }
}
