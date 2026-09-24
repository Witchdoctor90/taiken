namespace Taiken.Shared;

public sealed record VenueId(Guid Value) : StronglyTypedId(Value)
{
    public static VenueId New() => new(Guid.NewGuid());
}
