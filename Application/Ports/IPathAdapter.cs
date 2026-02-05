namespace Application.Ports;

public interface IPathAdapter
{
    string ToUNC(string path);
    bool IsValidPath(string path);
}
