namespace Application.Ports;

public interface IPriorityFileConfig
{
    IReadOnlyList<string> GetPriorityExtensions();
    void SetPriorityExtensions(IReadOnlyList<string> extensions);
}
