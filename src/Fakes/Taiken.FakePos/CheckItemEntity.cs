namespace Taiken.FakePos;

internal sealed class CheckItemEntity
{
    public int Id { get; init; }

    public int CheckId { get; set; }

    public required string Title { get; init; }

    public required decimal Qty { get; init; }

    public required decimal Price { get; init; }

    public List<CheckItemModifierEntity> Modifiers { get; init; } = [];
}

internal sealed class CheckItemModifierEntity
{
    public int Id { get; init; }

    public int CheckItemId { get; set; }

    public required string Title { get; init; }

    public required decimal Price { get; init; }
}
