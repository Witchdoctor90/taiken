using System.Text.Json.Serialization;

namespace Taiken.FakePos;

internal sealed record CheckDto(
    [property: JsonPropertyName("check_id")] string CheckId,
    [property: JsonPropertyName("rev")] int Rev,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("sum_total")] string SumTotal,
    [property: JsonPropertyName("items")] IReadOnlyList<CheckItemDto> Items,
    [property: JsonPropertyName("disc")] IReadOnlyList<CheckDiscountDto> Disc);

internal sealed record CheckItemDto(
    [property: JsonPropertyName("line")] string Line,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("qty")] string Qty,
    [property: JsonPropertyName("price")] string Price,
    [property: JsonPropertyName("mods")] IReadOnlyList<CheckItemModifierDto> Mods);

internal sealed record CheckItemModifierDto(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("price")] string Price);

internal sealed record CheckDiscountDto(
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("value")] string Value);

internal sealed record PaymentRequestDto(
    [property: JsonPropertyName("amount")] string Amount,
    [property: JsonPropertyName("ext_ref")] string ExtRef);

internal sealed record ErrorDto(
    [property: JsonPropertyName("error")] string Error);
