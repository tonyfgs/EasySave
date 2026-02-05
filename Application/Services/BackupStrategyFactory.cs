using Model;

namespace Application.Services;

public class BackupStrategyFactory
{
    public IBackupStrategy Create(BackupType type)
    {
        return type switch
        {
            BackupType.Full => new FullBackupStrategy(),
            BackupType.Differential => new DifferentialBackupStrategy(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unknown backup type: {type}")
        };
    }
}
