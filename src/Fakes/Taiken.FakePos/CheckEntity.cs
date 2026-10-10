namespace Taiken.FakePos;

/// <summary>
/// One "check" (bill) at one table. "Current" for a table means the single row with
/// <see cref="State"/> <c>OPEN</c> for that (location, table) pair — resetting a table retires
/// its old check (<c>CLOSED</c>, never deleted) and inserts a fresh one, rather than mutating it
/// in place, so history survives a reset.
/// </summary>
internal sealed class CheckEntity
{
    public int Id { get; init; }

    public required string LocationCode { get; init; }

    public required string TableNo { get; init; }

    public int Rev { get; set; } = 1;

    public CheckState State { get; set; } = CheckState.Open;

    public decimal SumTotal { get; set; }

    public string? PaidExtRef { get; set; }

    public decimal? PaidAmount { get; set; }

    public List<CheckItemEntity> Items { get; init; } = [];

    public List<CheckDiscountEntity> Discounts { get; init; } = [];

    public void Recalculate()
    {
        var itemsTotal = Items.Sum(i => i.Qty * i.Price + i.Modifiers.Sum(m => m.Price));
        var discountsTotal = Discounts.Sum(d => d.Value);
        SumTotal = itemsTotal + discountsTotal;
    }
}

internal enum CheckState
{
    Open,
    Paid,
    Closed,
}
