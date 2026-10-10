using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace Taiken.FakePos;

internal static class PosApiEndpoints
{
    public static async Task<IResult> GetCheck(string loc, string no, FakePosDbContext db, CancellationToken cancellationToken)
    {
        var check = await CurrentOpenCheckQuery(db, loc, no).FirstOrDefaultAsync(cancellationToken);
        return check is null ? Results.NotFound() : Results.Ok(CheckDtoMapper.ToDto(check));
    }

    public static async Task<IResult> RegisterPayment(
        string checkId,
        PaymentRequestDto body,
        FakePosDbContext db,
        CancellationToken cancellationToken)
    {
        if (!CheckIdFormat.TryParse(checkId, out var id))
        {
            return Results.NotFound();
        }

        var check = await FullCheckQuery(db).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (check is null)
        {
            return Results.NotFound();
        }

        if (!decimal.TryParse(body.Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            return Results.BadRequest(new ErrorDto("amount must be a decimal string."));
        }

        switch (check.State)
        {
            case CheckState.Paid:
                // Idempotent (TAI-23): replaying the same ext_ref against an already-paid check
                // is a no-op success, not an error.
                return check.PaidExtRef == body.ExtRef
                    ? Results.Ok(CheckDtoMapper.ToDto(check))
                    : Results.Json(new ErrorDto($"Check {checkId} was already paid with a different ext_ref."), statusCode: StatusCodes.Status409Conflict);

            case CheckState.Closed:
                return Results.Json(new ErrorDto($"Check {checkId} was reset and can no longer be paid."), statusCode: StatusCodes.Status409Conflict);

            case CheckState.Open:
            default:
                check.State = CheckState.Paid;
                check.PaidExtRef = body.ExtRef;
                check.PaidAmount = amount;
                await db.SaveChangesAsync(cancellationToken);
                return Results.Ok(CheckDtoMapper.ToDto(check));
        }
    }

    public static IQueryable<CheckEntity> CurrentOpenCheckQuery(FakePosDbContext db, string locationCode, string tableNo) =>
        FullCheckQuery(db)
            .Where(c => c.LocationCode == locationCode && c.TableNo == tableNo && c.State == CheckState.Open)
            .OrderByDescending(c => c.Id);

    private static IQueryable<CheckEntity> FullCheckQuery(FakePosDbContext db) =>
        db.Checks.Include(c => c.Items).ThenInclude(i => i.Modifiers).Include(c => c.Discounts);
}
