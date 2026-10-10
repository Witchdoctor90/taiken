using Microsoft.EntityFrameworkCore;

namespace Taiken.FakePos;

internal static class DemoSeed
{
    public const string LocationCode = "demo";

    public static async Task EnsureSeededAsync(FakePosDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Checks.AnyAsync(c => c.LocationCode == LocationCode, cancellationToken))
        {
            return;
        }

        db.Checks.Add(NewOpenCheck(
            LocationCode,
            "1",
            items:
            [
                ("Espresso", 2m, 150.00m, [("Oat milk", 50.00m)]),
                ("Croissant", 1m, 120.00m, []),
            ],
            discounts: [("Happy hour", -100.00m)]));

        db.Checks.Add(NewOpenCheck(
            LocationCode,
            "2",
            items: [("Americano", 1m, 160.00m, [])],
            discounts: []));

        db.Checks.Add(NewOpenCheck(LocationCode, "3", items: [], discounts: []));

        await db.SaveChangesAsync(cancellationToken);
    }

    public static CheckEntity NewOpenCheck(
        string locationCode,
        string tableNo,
        IReadOnlyList<(string Title, decimal Qty, decimal Price, IReadOnlyList<(string Title, decimal Price)> Modifiers)> items,
        IReadOnlyList<(string Label, decimal Value)> discounts)
    {
        var check = new CheckEntity
        {
            LocationCode = locationCode,
            TableNo = tableNo,
        };

        foreach (var (title, qty, price, modifiers) in items)
        {
            var item = new CheckItemEntity { Title = title, Qty = qty, Price = price };
            foreach (var (modTitle, modPrice) in modifiers)
            {
                item.Modifiers.Add(new CheckItemModifierEntity { Title = modTitle, Price = modPrice });
            }

            check.Items.Add(item);
        }

        foreach (var (label, value) in discounts)
        {
            check.Discounts.Add(new CheckDiscountEntity { Label = label, Value = value });
        }

        check.Recalculate();
        return check;
    }
}
