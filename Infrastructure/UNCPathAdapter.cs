using Application.Ports;

namespace Infrastructure;

public class UNCPathAdapter : IPathAdapter
{
    public string ToUNC(string path)
    {
        if (path.StartsWith("\\\\"))
            return path;

        var machineName = Environment.MachineName;
        var normalized = path.Replace("/", "\\").TrimStart('\\');

        // Convert drive letter to administrative share (C: -> C$)
        if (normalized.Length >= 2 && normalized[1] == ':')
            normalized = normalized[0] + "$" + normalized.Substring(2);

        return $"\\\\{machineName}\\{normalized}";
    }

    public bool IsValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var invalidChars = Path.GetInvalidPathChars();
        return !path.Any(c => invalidChars.Contains(c));
    }
}
