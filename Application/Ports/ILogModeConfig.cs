namespace Application.Ports;

public interface ILogModeConfig
{
    LogMode GetLogMode();

    void SetLogMode(LogMode mode);

    string GetCentralizedServerUrl();

    void SetCentralizedServerUrl(string url);
}

