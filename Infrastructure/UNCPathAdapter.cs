using Application.Ports;

namespace Infrastructure;

public class UNCPathAdapter : IPathAdapter
{
    public string ToUNC(string path)
    {
        if (path.StartsWith("\\\\"))
            return path;

        var machineName = Environment.MachineName;
        var normalized = path.Replace("/", "\\");
        return $"\\\\{machineName}\\{normalized.TrimStart('\\')}";
    }

    public bool IsValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var invalidChars = Path.GetInvalidPathChars();
        return !path.Any(c => invalidChars.Contains(c));
    }
}
