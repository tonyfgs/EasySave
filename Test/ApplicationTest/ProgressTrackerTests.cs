using Application.Services;
using Model;

namespace ApplicationTest;

public class ProgressTrackerTests
{
    private readonly ProgressTracker _tracker = new();

    [Fact]
    public void Initialize_ShouldSetTotals()
    {
        var files = new List<FileDescriptor>
        {
            new("/file1.txt", 100, DateTime.Now),
            new("/file2.txt", 200, DateTime.Now)
        };

        _tracker.Initialize(files);
        var snapshot = _tracker.BuildSnapshot("TestJob");

        Assert.Equal(2, snapshot.TotalFilesCount);
        Assert.Equal(300, snapshot.TotalFilesSize);
        Assert.Equal(0, snapshot.Progress);
    }

    [Fact]
    public void FileProcessed_ShouldUpdateProgress()
    {
        var files = new List<FileDescriptor>
        {
            new("/file1.txt", 100, DateTime.Now),
            new("/file2.txt", 100, DateTime.Now)
        };

        _tracker.Initialize(files);
        _tracker.FileProcessed(files[0]);

        Assert.Equal(50, _tracker.GetProgress());
    }

    [Fact]
    public void FileProcessed_AllFiles_ShouldBe100Percent()
    {
        var files = new List<FileDescriptor>
        {
            new("/file1.txt", 100, DateTime.Now),
            new("/file2.txt", 200, DateTime.Now)
        };

        _tracker.Initialize(files);
        _tracker.FileProcessed(files[0]);
        _tracker.FileProcessed(files[1]);

        Assert.Equal(100, _tracker.GetProgress());
    }

    [Fact]
    public void BuildSnapshot_ShouldReflectCurrentState()
    {
        var files = new List<FileDescriptor>
        {
            new("/file1.txt", 100, DateTime.Now),
            new("/file2.txt", 200, DateTime.Now),
            new("/file3.txt", 300, DateTime.Now)
        };

        _tracker.Initialize(files);
        _tracker.FileProcessed(files[0]);

        var snapshot = _tracker.BuildSnapshot("MyJob");

        Assert.Equal("MyJob", snapshot.Name);
        Assert.Equal(3, snapshot.TotalFilesCount);
        Assert.Equal(600, snapshot.TotalFilesSize);
        Assert.Equal(2, snapshot.RemainingFilesCount);
        Assert.Equal(500, snapshot.RemainingFilesSize);
        Assert.Equal(33, snapshot.Progress);
        Assert.Equal(JobState.Active, snapshot.State);
    }

    [Fact]
    public void Reset_ShouldClearAll()
    {
        var files = new List<FileDescriptor>
        {
            new("/file.txt", 100, DateTime.Now)
        };

        _tracker.Initialize(files);
        _tracker.FileProcessed(files[0]);
        _tracker.Reset();

        Assert.Equal(0, _tracker.GetProgress());
    }

    [Fact]
    public void GetProgress_NoFiles_ShouldReturnZero()
    {
        _tracker.Initialize(new List<FileDescriptor>());
        Assert.Equal(0, _tracker.GetProgress());
    }
}
