using Application.Ports;

namespace Infrastructure;

public class ProcessNameResolver : IProcessNameResolver
{
    private record SoftwareEntry(string Display, string Windows, string MacOS, string Linux);

    private static readonly IReadOnlyList<SoftwareEntry> SoftwareMapping =
    [
        new("Calculator", "CalculatorApp", "Calculator", "gnome-calculator"),
        new("Notepad", "notepad", "TextEdit", "gedit"),
        new("Word", "WINWORD", "Microsoft Word", "libreoffice"),
        new("Excel", "EXCEL", "Microsoft Excel", "libreoffice"),
        new("Chrome", "chrome", "Google Chrome", "google-chrome"),
        new("Firefox", "firefox", "firefox", "firefox"),
        new("Teams", "ms-teams", "MSTeams", "teams"),
        new("Slack", "slack", "Slack", "slack"),
    ];

    public string GetProcessName(string displayName)
    {
        var entry = SoftwareMapping.FirstOrDefault(e => e.Display == displayName);
        if (entry is null) return displayName;
        return OperatingSystem.IsWindows() ? entry.Windows
             : OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() ? entry.MacOS
             : entry.Linux;
    }

    public string GetDisplayName(string processName)
    {
        // Search only the current platform's column to avoid cross-platform collisions.
        var entry = SoftwareMapping.FirstOrDefault(e =>
            OperatingSystem.IsWindows() ? e.Windows == processName
            : OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() ? e.MacOS == processName
            : e.Linux == processName);
        return entry?.Display ?? processName;
    }

    public IReadOnlyList<string> GetAvailableDisplayNames()
        => SoftwareMapping.Select(e => e.Display).ToList();
}
