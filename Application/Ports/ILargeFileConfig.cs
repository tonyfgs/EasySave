namespace Application.Ports;

public interface ILargeFileConfig
{
    long GetLargeFileSizeThresholdKb();
    void SetLargeFileSizeThresholdKb(long thresholdKb);
}
