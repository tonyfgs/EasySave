using System.Diagnostics;
using Application.Ports;

namespace Infrastructure;

public class ProcessDetector : IBusinessSoftwareDetector
{
    private readonly IBusinessSoftwareConfig _config;

    public ProcessDetector(IBusinessSoftwareConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public BusinessSoftwareStatus GetStatus()
    {
        return BusinessSoftwareStatus.Disabled;
    }

    protected virtual Process[] FindProcesses(string name)
    {
        return Process.GetProcessesByName(name);
    }
}
