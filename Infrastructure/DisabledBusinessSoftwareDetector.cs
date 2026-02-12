using Application.Ports;

namespace Infrastructure;

public class DisabledBusinessSoftwareDetector : IBusinessSoftwareDetector
{
    public BusinessSoftwareStatus GetStatus() => BusinessSoftwareStatus.Disabled;
}
