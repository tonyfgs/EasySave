using Application.Ports;
using Infrastructure;
using Model;

namespace InfrastructureTest;

public class FileJobRepositoryTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _filePath;

    public FileJobRepositoryTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"easysave_repo_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _filePath = Path.Combine(_testDir, "jobs.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void Save_ShouldPersistJob_AndGetByIdReturnsIt()
    {
        IJobRepository repo = new FileJobRepository(_filePath);
        var job = new BackupJob(1, "Job1", "/src", "/dst", BackupType.Full);

        repo.Save(job);

        var retrieved = repo.GetById(1);
        Assert.NotNull(retrieved);
        Assert.Equal("Job1", retrieved!.Name);
    }

    [Fact]
    public void Save_WithIdZero_ShouldAutoGenerateId()
    {
        IJobRepository repo = new FileJobRepository(_filePath);
        var job = new BackupJob(0, "AutoId", "/src", "/dst", BackupType.Full);

        repo.Save(job);

        Assert.NotEqual(0, job.Id);
        Assert.Equal(1, repo.Count());
    }

    [Fact]
    public void Delete_ShouldRemoveJob()
    {
        IJobRepository repo = new FileJobRepository(_filePath);
        var job = new BackupJob(1, "ToDelete", "/src", "/dst", BackupType.Full);
        repo.Save(job);

        repo.Delete(1);

        Assert.Null(repo.GetById(1));
        Assert.Equal(0, repo.Count());
    }

    [Fact]
    public void GetAll_ShouldReturnAllSavedJobs()
    {
        IJobRepository repo = new FileJobRepository(_filePath);
        repo.Save(new BackupJob(1, "Job1", "/src1", "/dst1", BackupType.Full));
        repo.Save(new BackupJob(2, "Job2", "/src2", "/dst2", BackupType.Differential));

        var all = repo.GetAll();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void GetById_WithUnknownId_ShouldReturnNull()
    {
        IJobRepository repo = new FileJobRepository(_filePath);

        var result = repo.GetById(999);

        Assert.Null(result);
    }

    [Fact]
    public void Update_ShouldModifyPersistedJob()
    {
        IJobRepository repo = new FileJobRepository(_filePath);
        var job = new BackupJob(1, "Original", "/src", "/dst", BackupType.Full);
        repo.Save(job);

        job.Name = "Updated";
        repo.Update(job);

        var retrieved = repo.GetById(1);
        Assert.Equal("Updated", retrieved!.Name);
    }

    [Fact]
    public void Count_ShouldReturnCorrectCount()
    {
        IJobRepository repo = new FileJobRepository(_filePath);

        Assert.Equal(0, repo.Count());

        repo.Save(new BackupJob(1, "Job1", "/src1", "/dst1", BackupType.Full));
        Assert.Equal(1, repo.Count());

        repo.Save(new BackupJob(2, "Job2", "/src2", "/dst2", BackupType.Full));
        Assert.Equal(2, repo.Count());
    }

    [Fact]
    public void Persistence_DataSurvivesNewInstance()
    {
        var repo1 = new FileJobRepository(_filePath);
        repo1.Save(new BackupJob(1, "Persistent", "/src", "/dst", BackupType.Full));

        var repo2 = new FileJobRepository(_filePath);

        Assert.Equal(1, repo2.Count());
        Assert.Equal("Persistent", repo2.GetById(1)!.Name);
    }

    [Fact]
    public void Save_AfterDeletion_ShouldAssignMaxPlusOneId()
    {
        IJobRepository repo = new FileJobRepository(_filePath);
        repo.Save(new BackupJob(0, "Job1", "/s1", "/d1", BackupType.Full));
        repo.Save(new BackupJob(0, "Job2", "/s2", "/d2", BackupType.Full));
        repo.Save(new BackupJob(0, "Job3", "/s3", "/d3", BackupType.Full));

        repo.Delete(2);
        var newJob = new BackupJob(0, "Job4", "/s4", "/d4", BackupType.Full);
        repo.Save(newJob);

        Assert.Equal(4, newJob.Id);
        Assert.Equal(3, repo.Count());
    }

    [Fact]
    public void Save_AfterDeletingLastJob_ShouldAssignMaxPlusOneId()
    {
        IJobRepository repo = new FileJobRepository(_filePath);
        repo.Save(new BackupJob(0, "Job1", "/s1", "/d1", BackupType.Full));
        repo.Save(new BackupJob(0, "Job2", "/s2", "/d2", BackupType.Full));

        repo.Delete(2);
        var newJob = new BackupJob(0, "Job3", "/s3", "/d3", BackupType.Full);
        repo.Save(newJob);

        Assert.Equal(2, newJob.Id);
        Assert.Equal(2, repo.Count());
    }
}
