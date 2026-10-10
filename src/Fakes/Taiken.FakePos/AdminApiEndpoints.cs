using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace Taiken.FakePos;

/// <summary>
/// JSON API for the admin emulator (a separate Electron/React app, not server-rendered here) —
/// the same "staff at a POS terminal" actions as the public API's payments endpoint, just for
/// managing a table's check instead of closing it.
/// </summary>
internal static class AdminApiEndpoints
{
    public static async Task<IResult> Locations(FakePosDbContext db, CancellationToken cancellationToken)
    {
        var locations = await db.Checks.Select(c => c.LocationCode).Distinct().OrderBy(l => l).ToListAsync(cancellationToken);
        return Results.Ok(locations);
    }

    public static async Task<IResult> Tables(string loc, FakePosDbContext db, CancellationToken cancellationToken)
    {
        var tableNumbers = await db.Checks
            .Where(c => c.LocationCode == loc)
            .Select(c => c.TableNo)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(cancellationToken);

        var tables = new List<TableSummaryDto>();
        foreach (var tableNo in tableNumbers)
        {
            var current = await PosApiEndpoints.CurrentOpenCheckQuery(db, loc, tableNo).FirstOrDefaultAsync(cancellationToken);
            tables.Add(new TableSummaryDto(tableNo, current is null ? null : CheckDtoMapper.ToDto(current)));
        }

        return Results.Ok(tables);
    }

    public static async Task<IResult> TableDetail(string loc, string no, FakePosDbContext db, CancellationToken cancellationToken)
    {
        var current = await PosApiEndpoints.CurrentOpenCheckQuery(db, loc, no).FirstOrDefaultAsync(cancellationToken);
        return Results.Ok(new TableSummaryDto(no, current is null ? null : CheckDtoMapper.ToDto(current)));
    }

    public static async Task<IResult> AddItem(
        string loc,
        string no,
        AddItemRequestDto body,
        FakePosDbContext db,
        CancellationToken cancellationToken)
    {
        var check = await PosApiEndpoints.CurrentOpenCheckQuery(db, loc, no).FirstOrDefaultAsync(cancellationToken);
        if (check is null)
        {
            return Results.NotFound(new ErrorDto($"No open check for {loc}/{no}."));
        }

        if (!decimal.TryParse(body.Qty, NumberStyles.Number, CultureInfo.InvariantCulture, out var qty) ||
            !decimal.TryParse(body.Price, NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
        {
            return Results.BadRequest(new ErrorDto("qty and price must be decimal strings."));
        }

        check.Items.Add(new CheckItemEntity { Title = body.Title, Qty = qty, Price = price });
        check.Rev++;
        check.Recalculate();
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(CheckDtoMapper.ToDto(check));
    }

    public static async Task<IResult> ResetCheck(string loc, string no, FakePosDbContext db, CancellationToken cancellationToken)
    {
        var current = await PosApiEndpoints.CurrentOpenCheckQuery(db, loc, no).FirstOrDefaultAsync(cancellationToken);
        if (current is not null)
        {
            current.State = CheckState.Closed;
        }

        var fresh = DemoSeed.NewOpenCheck(loc, no, items: [], discounts: []);
        db.Checks.Add(fresh);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(CheckDtoMapper.ToDto(fresh));
    }
}
