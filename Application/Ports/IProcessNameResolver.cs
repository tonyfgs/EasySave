namespace Application.Ports;

public interface IProcessNameResolver
{
    /// <summary>Returns the OS-specific process name for a display name. Returns the raw string if not found.</summary>
    string GetProcessName(string displayName);

    /// <summary>Reverse-maps an OS process name to its display name. Returns the raw string if not found.</summary>
    string GetDisplayName(string processName);

    /// <summary>All display names available as suggestions.</summary>
    IReadOnlyList<string> GetAvailableDisplayNames();
}
