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

        Assert.Equal(2, snapshot.TotalFiles);
        Assert.Equal(300, snapshot.TotalSize);
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
        Assert.Equal(3, snapshot.TotalFiles);
        Assert.Equal(600, snapshot.TotalSize);
        Assert.Equal(2, snapshot.FilesRemaining);
        Assert.Equal(500, snapshot.SizeRemaining);
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

    [Fact]
    public void BuildSnapshot_DuringProcessing_ShouldIncludeCurrentFilePaths()
    {
        var files = new List<FileDescriptor>
        {
            new("/src/file1.txt", 100, DateTime.Now),
            new("/src/file2.txt", 200, DateTime.Now)
        };

        _tracker.Initialize(files);
        _tracker.SetCurrentFile("/src/file1.txt", "/dst/file1.txt");

        var snapshot = _tracker.BuildSnapshot("TestJob");

        Assert.Equal("/src/file1.txt", snapshot.CurrentSourceFile);
        Assert.Equal("/dst/file1.txt", snapshot.CurrentDestFile);
    }

    [Fact]
    public void BuildSnapshot_AfterReset_CurrentFilesShouldBeEmpty()
    {
        var files = new List<FileDescriptor>
        {
            new("/src/file1.txt", 100, DateTime.Now)
        };

        _tracker.Initialize(files);
        _tracker.SetCurrentFile("/src/file1.txt", "/dst/file1.txt");
        _tracker.Reset();

        var snapshot = _tracker.BuildSnapshot("TestJob");

        Assert.Equal(string.Empty, snapshot.CurrentSourceFile);
        Assert.Equal(string.Empty, snapshot.CurrentDestFile);
    }

    [Fact]
    public void BuildSnapshot_DefaultState_ShouldBeInactive()
    {
        var snapshot = _tracker.BuildSnapshot("TestJob");
        Assert.Equal(JobState.Inactive, snapshot.State);
    }

    [Fact]
    public void BuildSnapshot_AfterInitialize_ShouldBeActive()
    {
        var files = new List<FileDescriptor>
        {
            new("/file.txt", 100, DateTime.Now)
        };
        _tracker.Initialize(files);

        var snapshot = _tracker.BuildSnapshot("TestJob");
        Assert.Equal(JobState.Active, snapshot.State);
    }

    [Fact]
    public void BuildSnapshot_WhenStateSetToEnd_ShouldReturnEnd()
    {
        var files = new List<FileDescriptor>
        {
            new("/file.txt", 100, DateTime.Now)
        };
        _tracker.Initialize(files);
        _tracker.FileProcessed(files[0]);
        _tracker.SetState(JobState.End);

        var snapshot = _tracker.BuildSnapshot("TestJob");
        Assert.Equal(JobState.End, snapshot.State);
    }
}
