namespace Taiken.Shared;

/// <summary>An amount in minor units (e.g. cents) of a given <see cref="Currency"/>. Never a floating-point type.</summary>
public readonly record struct Money(long AmountMinor, Currency Currency);
