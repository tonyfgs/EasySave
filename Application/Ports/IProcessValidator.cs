namespace Application.Ports;

public interface IProcessValidator
{
    bool IsProcessRunning(string processName);
}
