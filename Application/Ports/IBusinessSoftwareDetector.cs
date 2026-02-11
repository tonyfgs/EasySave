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
