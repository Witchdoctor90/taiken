using Taiken.Shared;

namespace Taiken.Shared.Tests;

public class MoneyTests
{
    [Fact]
    public void Equal_amount_and_currency_are_equal()
    {
        Assert.Equal(new Money(1_000, Currency.Eur), new Money(1_000, Currency.Eur));
    }

    [Fact]
    public void Same_amount_in_different_currencies_are_not_equal()
    {
        Assert.NotEqual(new Money(1_000, Currency.Eur), new Money(1_000, Currency.All));
    }

    [Theory]
    [InlineData("ALL", 2)]
    [InlineData("EUR", 2)]
    public void Currency_exposes_its_own_minor_unit_exponent(string code, int expectedMinorUnitDigits)
    {
        var currency = code == "ALL" ? Currency.All : Currency.Eur;

        Assert.Equal(expectedMinorUnitDigits, currency.MinorUnitDigits);
    }
}
