using RedPandaFlow.Application.Common;

namespace RedPandaFlow.Tests;

public class ServiceResultTests
{
    [Fact]
    public void Ok_SetsSuccessDataAndNoError()
    {
        var result = ServiceResult<int>.Ok(42, "done");

        Assert.True(result.Success);
        Assert.Equal(42, result.Data);
        Assert.Equal("done", result.Message);
        Assert.Equal(ServiceErrorType.None, result.ErrorType);
    }

    [Fact]
    public void Fail_SetsErrorAndLeavesDataDefault()
    {
        var result = ServiceResult<string>.Fail("not found", ServiceErrorType.NotFound);

        Assert.False(result.Success);
        Assert.Equal("not found", result.Message);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Null(result.Data);
    }
}
