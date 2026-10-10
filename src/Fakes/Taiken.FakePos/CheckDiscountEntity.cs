namespace Taiken.FakePos;

/// <summary><see cref="Value"/> is negative — it's added straight into <see cref="CheckEntity.SumTotal"/>, same as the wire format's <c>disc[].value</c>.</summary>
internal sealed class CheckDiscountEntity
{
    public int Id { get; init; }

    public int CheckId { get; set; }

    public required string Label { get; init; }

    public required decimal Value { get; init; }
}
