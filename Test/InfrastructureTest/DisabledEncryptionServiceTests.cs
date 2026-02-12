using Application.Ports;
using Infrastructure;

namespace InfrastructureTest;

public class DisabledEncryptionServiceTests
{
    private readonly DisabledEncryptionService _service = new();

    [Fact]
    public void EncryptFile_ShouldReturnSuccessWithZeroDuration()
    {
        var result = _service.EncryptFile("/test/file.txt");

        Assert.True(result.Success);
        Assert.Equal(0, result.DurationMs);
        Assert.Equal(CryptoErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void DecryptFile_ShouldReturnSuccessWithZeroDuration()
    {
        var result = _service.DecryptFile("/test/file.txt");

        Assert.True(result.Success);
        Assert.Equal(0, result.DurationMs);
        Assert.Equal(CryptoErrorCode.None, result.ErrorCode);
    }
}
