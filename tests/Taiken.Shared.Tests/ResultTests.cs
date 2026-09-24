using Taiken.Shared;

namespace Taiken.Shared.Tests;

public class ResultTests
{
    [Fact]
    public void Success_has_no_error()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_carries_the_given_error()
    {
        var error = new Error("venue.not_found", "Venue not found.");

        var result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Success_of_T_exposes_the_value()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Failure_of_T_throws_when_value_is_accessed()
    {
        var error = new Error("venue.not_found", "Venue not found.");
        var result = Result.Failure<int>(error);

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Value_implicitly_converts_to_a_successful_result()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Error_implicitly_converts_to_a_failed_result()
    {
        var error = new Error("venue.not_found", "Venue not found.");

        Result<int> result = error;

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }
}
