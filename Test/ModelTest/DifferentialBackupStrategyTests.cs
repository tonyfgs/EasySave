using Model;

namespace ModelTest;

public class DifferentialBackupStrategyTests
{
    [Fact]
    public void SelectFiles_NoLastFullBackup_ShouldReturnAllFiles()
    {
        var strategy = new DifferentialBackupStrategy();
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Differential);
        var files = new List<FileDescriptor>
        {
            new("/file1.txt", 100, DateTime.Now),
            new("/file2.txt", 200, DateTime.Now)
        };

        var result = strategy.SelectFiles(job, files);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void SelectFiles_WithLastFullBackup_ShouldReturnOnlyModifiedFiles()
    {
        var strategy = new DifferentialBackupStrategy();
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Differential);
        job.MarkFullBackupCompleted(new DateTime(2025, 6, 1));

        var files = new List<FileDescriptor>
        {
            new("/old.txt", 100, new DateTime(2025, 1, 1)),
            new("/new.txt", 200, new DateTime(2025, 7, 1)),
            new("/newer.txt", 300, new DateTime(2025, 8, 1))
        };

        var result = strategy.SelectFiles(job, files);

        Assert.Equal(2, result.Count);
        Assert.All(result, f => Assert.True(f.LastModified > new DateTime(2025, 6, 1)));
    }

    [Fact]
    public void SelectFiles_AllFilesOlderThanLastFull_ShouldReturnEmpty()
    {
        var strategy = new DifferentialBackupStrategy();
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Differential);
        job.MarkFullBackupCompleted(new DateTime(2025, 12, 31));

        var files = new List<FileDescriptor>
        {
            new("/old1.txt", 100, new DateTime(2025, 1, 1)),
            new("/old2.txt", 200, new DateTime(2025, 6, 1))
        };

        var result = strategy.SelectFiles(job, files);

        Assert.Empty(result);
    }
}
