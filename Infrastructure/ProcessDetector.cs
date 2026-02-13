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
        if (!_config.IsDetectionEnabled())
            return BusinessSoftwareStatus.Disabled;

        var name = _config.GetBusinessSoftwareName();
        if (string.IsNullOrWhiteSpace(name))
            return BusinessSoftwareStatus.Disabled;

        try
        {
            var processes = FindProcesses(name);
            var isRunning = processes.Length > 0;
            foreach (var process in processes)
                process.Dispose();
            return isRunning
                ? BusinessSoftwareStatus.Running
                : BusinessSoftwareStatus.NotRunning;
        }
        catch
        {
            return BusinessSoftwareStatus.Error;
        }
    }

    protected virtual Process[] FindProcesses(string name)
    {
        return Process.GetProcessesByName(name);
    }
}
