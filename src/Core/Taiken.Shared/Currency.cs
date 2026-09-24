namespace Taiken.Shared;

public sealed record Currency
{
    private Currency(string code, int minorUnitDigits)
    {
        Code = code;
        MinorUnitDigits = minorUnitDigits;
    }

    /// <summary>ISO 4217 alphabetic code, e.g. "EUR".</summary>
    public string Code { get; }

    /// <summary>ISO 4217 minor unit exponent — how many digits <see cref="Money.AmountMinor"/> represents after the decimal point.</summary>
    public int MinorUnitDigits { get; }

    public static readonly Currency All = new("ALL", 2);

    public static readonly Currency Eur = new("EUR", 2);
}
