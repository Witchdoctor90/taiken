using System.Text.Json.Serialization;

namespace Taiken.FakePsp;

internal sealed record CreateSessionRequestDto(
    [property: JsonPropertyName("amount_major")] string AmountMajor,
    [property: JsonPropertyName("ccy")] string Ccy,
    [property: JsonPropertyName("merchant")] string Merchant,
    [property: JsonPropertyName("order_ref")] string OrderRef,
    [property: JsonPropertyName("callback_url")] string CallbackUrl,
    [property: JsonPropertyName("embed_origin")] string EmbedOrigin);

internal sealed record CreateSessionResponseDto(
    [property: JsonPropertyName("sid")] string Sid,
    [property: JsonPropertyName("pay_url")] string PayUrl,
    [property: JsonPropertyName("ttl")] int Ttl);

internal sealed record SessionStatusResponseDto(
    [property: JsonPropertyName("sid")] string Sid,
    [property: JsonPropertyName("st")] string St,
    [property: JsonPropertyName("paid")] string Paid);

internal sealed record WebhookPayloadDto(
    [property: JsonPropertyName("sid")] string Sid,
    [property: JsonPropertyName("st")] string St,
    [property: JsonPropertyName("paid")] string Paid,
    [property: JsonPropertyName("ts")] long Ts);

internal sealed record ScenarioResultDto(
    [property: JsonPropertyName("sid")] string Sid,
    [property: JsonPropertyName("st")] string St);

internal sealed record ErrorDto(
    [property: JsonPropertyName("error")] string Error);
