using System.Text.Json.Serialization;

namespace Taiken.FakePos;

internal sealed record TableSummaryDto(
    [property: JsonPropertyName("table_no")] string TableNo,
    [property: JsonPropertyName("check")] CheckDto? Check);

internal sealed record AddItemRequestDto(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("qty")] string Qty,
    [property: JsonPropertyName("price")] string Price);
