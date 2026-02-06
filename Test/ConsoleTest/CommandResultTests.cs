using EasySave.Commands;

namespace ConsoleTest;

public class CommandResultTests
{
    [Fact]
    public void Ok_ShouldReturnSuccessfulResult()
    {
        var result = CommandResult.Ok();

        Assert.True(result.IsSuccess());
    }

    [Fact]
    public void Fail_ShouldReturnFailedResult()
    {
        var result = CommandResult.Fail("Something went wrong");

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public void Fail_ShouldContainErrorMessage()
    {
        var result = CommandResult.Fail("Disk full");

        Assert.Equal("Disk full", result.GetErrorMessage());
    }

    [Fact]
    public void Ok_ShouldHaveEmptyErrorMessage()
    {
        var result = CommandResult.Ok();

        Assert.Equal(string.Empty, result.GetErrorMessage());
    }

    [Fact]
    public void Fail_WithNullMessage_ShouldUseEmptyString()
    {
        var result = CommandResult.Fail(null!);

        Assert.False(result.IsSuccess());
        Assert.Equal(string.Empty, result.GetErrorMessage());
    }
}
