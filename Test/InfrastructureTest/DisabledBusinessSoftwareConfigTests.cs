using Infrastructure;

namespace InfrastructureTest;

public class DisabledBusinessSoftwareConfigTests
{
    private readonly DisabledBusinessSoftwareConfig _config = new();

    [Fact]
    public void IsDetectionEnabled_ShouldReturnFalse()
    {
        Assert.False(_config.IsDetectionEnabled());
    }

    [Fact]
    public void GetBusinessSoftwareName_ShouldReturnEmpty()
    {
        Assert.Equal(string.Empty, _config.GetBusinessSoftwareName());
    }
}
