using Application.Ports;

namespace ApplicationTest;

public class CryptoResultTests
{
    [Fact]
    public void CryptoResult_Success_ShouldHaveDefaultValues()
    {
        var result = new CryptoResult { Success = true, DurationMs = 150 };

        Assert.True(result.Success);
        Assert.Equal(150, result.DurationMs);
        Assert.Equal(CryptoErrorCode.None, result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void CryptoResult_Failure_ShouldStoreErrorDetails()
    {
        var result = new CryptoResult
        {
            Success = false,
            DurationMs = -1,
            ErrorCode = CryptoErrorCode.FileNotFound,
            ErrorMessage = "File not found"
        };

        Assert.False(result.Success);
        Assert.Equal(-1, result.DurationMs);
        Assert.Equal(CryptoErrorCode.FileNotFound, result.ErrorCode);
        Assert.Equal("File not found", result.ErrorMessage);
    }

    [Theory]
    [InlineData(CryptoErrorCode.None)]
    [InlineData(CryptoErrorCode.FileNotFound)]
    [InlineData(CryptoErrorCode.InvalidArguments)]
    [InlineData(CryptoErrorCode.IoError)]
    [InlineData(CryptoErrorCode.AuthTagInvalid)]
    [InlineData(CryptoErrorCode.InvalidKey)]
    [InlineData(CryptoErrorCode.Timeout)]
    [InlineData(CryptoErrorCode.Unknown)]
    public void CryptoErrorCode_ShouldContainAllExpectedValues(CryptoErrorCode code)
    {
        Assert.True(Enum.IsDefined(typeof(CryptoErrorCode), code));
    }

    [Theory]
    [InlineData(BusinessSoftwareStatus.Disabled)]
    [InlineData(BusinessSoftwareStatus.NotRunning)]
    [InlineData(BusinessSoftwareStatus.Running)]
    [InlineData(BusinessSoftwareStatus.Unknown)]
    [InlineData(BusinessSoftwareStatus.Error)]
    public void BusinessSoftwareStatus_ShouldContainAllExpectedValues(BusinessSoftwareStatus status)
    {
        Assert.True(Enum.IsDefined(typeof(BusinessSoftwareStatus), status));
    }
}
