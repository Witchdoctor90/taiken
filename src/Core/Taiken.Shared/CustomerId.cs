namespace Taiken.Shared;

public sealed record CustomerId(Guid Value) : StronglyTypedId(Value)
{
    public static CustomerId New() => new(Guid.NewGuid());
}
