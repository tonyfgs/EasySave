using Application.Ports;

namespace Infrastructure;

public class DisabledBusinessSoftwareConfig : IBusinessSoftwareConfig
{
    public string GetBusinessSoftwareName() => string.Empty;
    public void SetBusinessSoftwareName(string name) { }

    public bool IsDetectionEnabled() => false;
    public void SetDetectionEnabled(bool enabled) { }
}
