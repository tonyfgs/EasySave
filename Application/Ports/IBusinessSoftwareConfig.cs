namespace Application.Ports;

public interface IBusinessSoftwareConfig
{
    string GetBusinessSoftwareName();
    void SetBusinessSoftwareName(string name);

    bool IsDetectionEnabled();
    void SetDetectionEnabled(bool enabled);
}
