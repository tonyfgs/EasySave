using Model;

namespace ModelTest;

public class BackupJobTests
{
    private BackupJob CreateValidJob(
        int id = 1,
        string name = "TestJob",
        string source = "/source/path",
        string target = "/target/path",
        BackupType type = BackupType.Full)
    {
        return new BackupJob(id, name, source, target, type);
    }

    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var job = CreateValidJob(1, "MyJob", "/src", "/dst", BackupType.Differential);

        Assert.Equal(1, job.Id);
        Assert.Equal("MyJob", job.Name);
        Assert.Equal("/src", job.SourcePath);
        Assert.Equal("/dst", job.TargetPath);
        Assert.Equal(BackupType.Differential, job.Type);
        Assert.Null(job.LastFullBackupDate);
        Assert.True(job.CreatedDate <= DateTime.Now);
        Assert.True(job.CreatedDate > DateTime.Now.AddSeconds(-5));
    }

    [Fact]
    public void Validate_ValidJob_ShouldNotThrow()
    {
        var job = CreateValidJob();
        var exception = Record.Exception(() => job.Validate());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("", "/src", "/dst")]
    [InlineData("  ", "/src", "/dst")]
    [InlineData(null, "/src", "/dst")]
    public void Validate_EmptyName_ShouldThrowInvalidBackupJobException(
        string? name, string source, string target)
    {
        var job = new BackupJob(1, name!, source, target, BackupType.Full);
        Assert.Throws<InvalidBackupJobException>(() => job.Validate());
    }

    [Theory]
    [InlineData("Job", "", "/dst")]
    [InlineData("Job", null, "/dst")]
    [InlineData("Job", "  ", "/dst")]
    public void Validate_EmptySourcePath_ShouldThrowInvalidBackupJobException(
        string name, string? source, string target)
    {
        var job = new BackupJob(1, name, source!, target, BackupType.Full);
        Assert.Throws<InvalidBackupJobException>(() => job.Validate());
    }

    [Theory]
    [InlineData("Job", "/src", "")]
    [InlineData("Job", "/src", null)]
    [InlineData("Job", "/src", "  ")]
    public void Validate_EmptyTargetPath_ShouldThrowInvalidBackupJobException(
        string name, string source, string? target)
    {
        var job = new BackupJob(1, name, source, target!, BackupType.Full);
        Assert.Throws<InvalidBackupJobException>(() => job.Validate());
    }

    [Fact]
    public void Validate_SourceEqualsTarget_ShouldThrowInvalidBackupJobException()
    {
        var job = new BackupJob(1, "Job", "/same/path", "/same/path", BackupType.Full);
        Assert.Throws<InvalidBackupJobException>(() => job.Validate());
    }

    [Fact]
    public void IsFileModifiedSinceLastFull_NoLastFullBackup_ShouldReturnTrue()
    {
        var job = CreateValidJob();
        var file = new FileDescriptor("/file.txt", 100, DateTime.Now);

        Assert.True(job.IsFileModifiedSinceLastFull(file));
    }

    [Fact]
    public void IsFileModifiedSinceLastFull_FileModifiedAfterLastFull_ShouldReturnTrue()
    {
        var job = CreateValidJob();
        job.MarkFullBackupCompleted(new DateTime(2025, 1, 1));
        var file = new FileDescriptor("/file.txt", 100, new DateTime(2025, 6, 1));

        Assert.True(job.IsFileModifiedSinceLastFull(file));
    }

    [Fact]
    public void IsFileModifiedSinceLastFull_FileModifiedBeforeLastFull_ShouldReturnFalse()
    {
        var job = CreateValidJob();
        job.MarkFullBackupCompleted(new DateTime(2025, 6, 1));
        var file = new FileDescriptor("/file.txt", 100, new DateTime(2025, 1, 1));

        Assert.False(job.IsFileModifiedSinceLastFull(file));
    }

    [Fact]
    public void IsFileModifiedSinceLastFull_FileModifiedExactlyAtLastFull_ShouldReturnFalse()
    {
        var exactDate = new DateTime(2025, 6, 1, 12, 0, 0);
        var job = CreateValidJob();
        job.MarkFullBackupCompleted(exactDate);
        var file = new FileDescriptor("/file.txt", 100, exactDate);

        Assert.False(job.IsFileModifiedSinceLastFull(file));
    }

    [Fact]
    public void MarkFullBackupCompleted_ShouldUpdateLastFullBackupDate()
    {
        var job = CreateValidJob();
        var date = new DateTime(2025, 3, 15);

        job.MarkFullBackupCompleted(date);

        Assert.Equal(date, job.LastFullBackupDate);
    }
}
