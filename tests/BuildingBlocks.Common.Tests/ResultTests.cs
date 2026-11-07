using BuildingBlocks.Common.Results;
using Xunit;

namespace BuildingBlocks.Common.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Success_ISSuccess_IsTrue()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void Failure_HasError()
    {
        var error = new Error("CODE", "msg");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal("CODE", result.Error.Code);
    }

    [Fact]
    public void GenericSuccess_ReturnsValue()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_AccessingValue_Throws()
    {
        var result = Result.Failure<int>(new Error("X", "y"));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
