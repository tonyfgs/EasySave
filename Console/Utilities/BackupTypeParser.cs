using Model;

namespace EasySave.Utilities;

public static class BackupTypeParser
{
    public static BackupType Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || int.TryParse(input, out _))
            throw new ArgumentException(
                $"Invalid backup type: '{input}'. Expected 'Full' or 'Differential'.");

        if (!Enum.TryParse<BackupType>(input, ignoreCase: true, out var result))
            throw new ArgumentException(
                $"Invalid backup type: '{input}'. Expected 'Full' or 'Differential'.");

        return result;
    }
}
