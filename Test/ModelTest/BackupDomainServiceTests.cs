using Model;

namespace ModelTest;

public class BackupDomainServiceTests
{
    private readonly BackupDomainService _service = new();

    [Fact]
    public void ValidateJobLimit_UnderLimit_ShouldNotThrow()
    {
        var exception = Record.Exception(() => _service.ValidateJobLimit(3, 5));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateJobLimit_AtLimit_ShouldThrow()
    {
        Assert.Throws<JobLimitExceededException>(() => _service.ValidateJobLimit(5, 5));
    }

    [Fact]
    public void ValidateJobLimit_OverLimit_ShouldThrow()
    {
        Assert.Throws<JobLimitExceededException>(() => _service.ValidateJobLimit(6, 5));
    }

    [Fact]
    public void ValidateJobLimit_ZeroCurrent_ShouldNotThrow()
    {
        var exception = Record.Exception(() => _service.ValidateJobLimit(0, 5));
        Assert.Null(exception);
    }

    [Fact]
    public void SelectFilesForBackup_ShouldValidateJobAndDelegateToStrategy()
    {
        var job = new BackupJob(1, "Job", "/src", "/dst", BackupType.Full);
        var files = new List<FileDescriptor>
        {
            new("/file1.txt", 100, DateTime.Now),
            new("/file2.txt", 200, DateTime.Now)
        };
        var strategy = new FullBackupStrategy();

        var result = _service.SelectFilesForBackup(job, files, strategy);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void SelectFilesForBackup_InvalidJob_ShouldThrow()
    {
        var job = new BackupJob(1, "", "/src", "/dst", BackupType.Full);
        var files = new List<FileDescriptor>();
        var strategy = new FullBackupStrategy();

        Assert.Throws<InvalidBackupJobException>(
            () => _service.SelectFilesForBackup(job, files, strategy));
    }

    [Fact]
    public void SelectFilesForBackup_WithDifferentialStrategy_ShouldFilterFiles()
    {
        var job = new BackupJob(1, "Job", "/src", "/dst", BackupType.Differential);
        job.MarkFullBackupCompleted(new DateTime(2025, 6, 1));

        var files = new List<FileDescriptor>
        {
            new("/old.txt", 100, new DateTime(2025, 1, 1)),
            new("/new.txt", 200, new DateTime(2025, 7, 1))
        };
        var strategy = new DifferentialBackupStrategy();

        var result = _service.SelectFilesForBackup(job, files, strategy);

        Assert.Single(result);
        Assert.Equal("/new.txt", result[0].Path);
    }
}
