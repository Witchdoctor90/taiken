namespace Taiken.Shared;

public sealed record PaymentIntentId(Guid Value) : StronglyTypedId(Value)
{
    public static PaymentIntentId New() => new(Guid.NewGuid());
}
