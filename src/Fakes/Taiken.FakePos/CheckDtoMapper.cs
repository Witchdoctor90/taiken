using System.Globalization;

namespace Taiken.FakePos;

internal static class CheckDtoMapper
{
    public static CheckDto ToDto(CheckEntity check) => new(
        CheckIdFormat.Format(check.Id),
        check.Rev,
        check.State.ToString().ToUpperInvariant(),
        FormatDecimal(check.SumTotal),
        check.Items
            .Select((item, index) => new CheckItemDto(
                $"L{index + 1}",
                item.Title,
                FormatQty(item.Qty),
                FormatDecimal(item.Price),
                item.Modifiers.Select(m => new CheckItemModifierDto(m.Title, FormatDecimal(m.Price))).ToList()))
            .ToList(),
        check.Discounts.Select(d => new CheckDiscountDto(d.Label, FormatDecimal(d.Value))).ToList());

    private static string FormatDecimal(decimal value) => value.ToString("F2", CultureInfo.InvariantCulture);

    private static string FormatQty(decimal qty) => qty == Math.Truncate(qty)
        ? ((long)qty).ToString(CultureInfo.InvariantCulture)
        : qty.ToString("0.##", CultureInfo.InvariantCulture);
}
