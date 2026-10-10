namespace Taiken.FakePsp;

internal static class PayEndpoints
{
    public static IResult GetPayPage(string sid, HttpResponse response, SessionStore store)
    {
        var session = store.Find(sid);
        if (session is null)
        {
            return Results.NotFound();
        }

        response.Headers["Content-Security-Policy"] = $"frame-ancestors {session.EmbedOrigin}";
        return Results.Content(PayPageHtml.Render(session), "text/html");
    }

    public static async Task<IResult> ApplyScenario(
        string sid,
        string scenario,
        SessionStore store,
        WebhookSender webhookSender,
        CancellationToken cancellationToken)
    {
        var session = store.Find(sid);
        if (session is null)
        {
            return Results.NotFound();
        }

        if (!Scenario.Known.Contains(scenario))
        {
            return Results.BadRequest(new ErrorDto($"Unknown scenario '{scenario}'."));
        }

        var webhookDeliveries = Scenario.Apply(session, scenario);
        for (var i = 0; i < webhookDeliveries; i++)
        {
            await webhookSender.SendAsync(session, cancellationToken);
        }

        return Results.Ok(new ScenarioResultDto(session.Sid, SessionStatusCodes.ToCode(session.Status)));
    }
}
