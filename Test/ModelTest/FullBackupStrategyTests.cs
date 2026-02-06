using Model;

namespace ModelTest;

public class FullBackupStrategyTests
{
    [Fact]
    public void SelectFiles_ShouldReturnAllFiles()
    {
        var strategy = new FullBackupStrategy();
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Full);
        var files = new List<FileDescriptor>
        {
            new("/file1.txt", 100, DateTime.Now),
            new("/file2.txt", 200, DateTime.Now),
            new("/file3.txt", 300, DateTime.Now)
        };

        var result = strategy.SelectFiles(job, files);

        Assert.Equal(3, result.Count);
        Assert.Equal(files, result);
    }

    [Fact]
    public void SelectFiles_EmptyList_ShouldReturnEmpty()
    {
        var strategy = new FullBackupStrategy();
        var job = new BackupJob(1, "TestJob", "/src", "/dst", BackupType.Full);

        var result = strategy.SelectFiles(job, new List<FileDescriptor>());

        Assert.Empty(result);
    }
}
