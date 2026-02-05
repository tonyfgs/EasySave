using Application.Services;
using Model;

namespace ApplicationTest;

public class BackupStrategyFactoryTests
{
    private readonly BackupStrategyFactory _factory = new();

    [Fact]
    public void Create_Full_ShouldReturnFullBackupStrategy()
    {
        var strategy = _factory.Create(BackupType.Full);
        Assert.IsType<FullBackupStrategy>(strategy);
    }

    [Fact]
    public void Create_Differential_ShouldReturnDifferentialBackupStrategy()
    {
        var strategy = _factory.Create(BackupType.Differential);
        Assert.IsType<DifferentialBackupStrategy>(strategy);
    }
}
