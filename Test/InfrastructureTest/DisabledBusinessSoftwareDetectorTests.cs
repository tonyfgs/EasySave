using Application.Ports;
using Infrastructure;

namespace InfrastructureTest;

public class DisabledBusinessSoftwareDetectorTests
{
    [Fact]
    public void GetStatus_ShouldReturnDisabled()
    {
        var detector = new DisabledBusinessSoftwareDetector();
        Assert.Equal(BusinessSoftwareStatus.Disabled, detector.GetStatus());
    }
}
