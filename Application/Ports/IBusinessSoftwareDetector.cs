namespace Application.Ports;

public interface IBusinessSoftwareDetector
{
    BusinessSoftwareStatus GetStatus();
}

public enum BusinessSoftwareStatus
{
    Disabled,
    NotRunning,
    Running,
    Unknown,
    Error
}

public static class BusinessSoftwareStatusExtensions
{
    public static bool IsBlocking(this BusinessSoftwareStatus status) =>
        status is BusinessSoftwareStatus.Running
            or BusinessSoftwareStatus.Unknown
            or BusinessSoftwareStatus.Error;
}
