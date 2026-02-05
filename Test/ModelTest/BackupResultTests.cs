using Model;

namespace ModelTest;

public class BackupResultTests
{
    [Fact]
    public void Ok_ShouldCreateSuccessResult()
    {
        var duration = TimeSpan.FromSeconds(5);
        var result = BackupResult.Ok(10, 2048, duration);

        Assert.True(result.Success);
        Assert.Equal(10, result.FilesProcessed);
        Assert.Equal(2048, result.BytesTransferred);
        Assert.Equal(duration, result.Duration);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Fail_ShouldCreateFailureResult()
    {
        var errors = new List<string> { "File not found", "Access denied" };
        var duration = TimeSpan.FromSeconds(2);
        var result = BackupResult.Fail(errors, duration);

        Assert.False(result.Success);
        Assert.Equal(0, result.FilesProcessed);
        Assert.Equal(0, result.BytesTransferred);
        Assert.Equal(duration, result.Duration);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("File not found", result.Errors);
    }

    [Fact]
    public void Errors_ShouldBeReadOnly()
    {
        var errors = new List<string> { "error" };
        var result = BackupResult.Fail(errors, TimeSpan.Zero);

        errors.Add("another error");
        Assert.Single(result.Errors);
    }

    [Fact]
    public void EqualResults_ShouldBeEqual()
    {
        var duration = TimeSpan.FromSeconds(5);
        var r1 = BackupResult.Ok(10, 2048, duration);
        var r2 = BackupResult.Ok(10, 2048, duration);

        Assert.Equal(r1, r2);
    }
}
