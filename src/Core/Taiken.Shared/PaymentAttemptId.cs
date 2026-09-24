namespace Taiken.Shared;

public sealed record PaymentAttemptId(Guid Value) : StronglyTypedId(Value)
{
    public static PaymentAttemptId New() => new(Guid.NewGuid());
}
