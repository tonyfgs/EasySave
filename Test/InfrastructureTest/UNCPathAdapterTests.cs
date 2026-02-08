using System.Net;
using Application.Ports;
using Infrastructure;

namespace InfrastructureTest;

public class UNCPathAdapterTests
{
    private readonly IPathAdapter _adapter = new UNCPathAdapter();

    [Fact]
    public void ToUNC_WithAbsolutePath_ShouldConvertToUNCFormat()
    {
        var result = _adapter.ToUNC("/Users/test/backup");

        Assert.StartsWith("\\\\", result);
    }

    [Fact]
    public void ToUNC_WithAlreadyUNCPath_ShouldReturnAsIs()
    {
        var uncPath = "\\\\server\\share\\folder";

        var result = _adapter.ToUNC(uncPath);

        Assert.Equal(uncPath, result);
    }

    [Fact]
    public void ToUNC_WithWindowsDriveLetter_ShouldConvertToAdminShare()
    {
        var result = _adapter.ToUNC("C:\\Users\\Jean\\Documents");

        var machineName = Dns.GetHostName();
        Assert.Equal($"\\\\{machineName}\\C$\\Users\\Jean\\Documents", result);
    }

    [Fact]
    public void IsValidPath_WithValidPath_ShouldReturnTrue()
    {
        Assert.True(_adapter.IsValidPath("/Users/test/backup"));
    }

    [Fact]
    public void IsValidPath_WithEmptyPath_ShouldReturnFalse()
    {
        Assert.False(_adapter.IsValidPath(""));
    }

    [Fact]
    public void IsValidPath_WithNullPath_ShouldReturnFalse()
    {
        Assert.False(_adapter.IsValidPath(null!));
    }

    [Fact]
    public void IsValidPath_WithInvalidChars_ShouldReturnFalse()
    {
        Assert.False(_adapter.IsValidPath("path\0with\0nulls"));
    }
}
