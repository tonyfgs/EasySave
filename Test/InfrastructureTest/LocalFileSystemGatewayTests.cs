using Application.Ports;
using Infrastructure;
using Model;

namespace InfrastructureTest;

public class LocalFileSystemGatewayTests : IDisposable
{
    private readonly string _testDir;

    public LocalFileSystemGatewayTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"easysave_fs_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void EnumerateFiles_ShouldReturnFileDescriptors()
    {
        var filePath = Path.Combine(_testDir, "test.txt");
        File.WriteAllText(filePath, "hello world");
        IFileSystemGateway gateway = new LocalFileSystemGateway();

        var files = gateway.EnumerateFiles(_testDir);

        Assert.Single(files);
        Assert.Equal(filePath, files[0].Path);
        Assert.True(files[0].Size > 0);
    }

    [Fact]
    public void EnumerateFiles_EmptyDir_ShouldReturnEmpty()
    {
        IFileSystemGateway gateway = new LocalFileSystemGateway();

        var files = gateway.EnumerateFiles(_testDir);

        Assert.Empty(files);
    }

    [Fact]
    public void EnumerateFiles_Recursive_ShouldIncludeSubdirectoryFiles()
    {
        var subDir = Path.Combine(_testDir, "sub");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(_testDir, "root.txt"), "root");
        File.WriteAllText(Path.Combine(subDir, "nested.txt"), "nested");
        IFileSystemGateway gateway = new LocalFileSystemGateway();

        var files = gateway.EnumerateFiles(_testDir);

        Assert.Equal(2, files.Count);
    }

    [Fact]
    public void EnsureDirectory_ShouldCreateDirectoryIfMissing()
    {
        var newDir = Path.Combine(_testDir, "new", "nested");
        IFileSystemGateway gateway = new LocalFileSystemGateway();

        gateway.EnsureDirectory(newDir);

        Assert.True(Directory.Exists(newDir));
    }

    [Fact]
    public void EnsureDirectory_ExistingDir_ShouldNotThrow()
    {
        IFileSystemGateway gateway = new LocalFileSystemGateway();

        var exception = Record.Exception(() => gateway.EnsureDirectory(_testDir));

        Assert.Null(exception);
    }

    [Fact]
    public void CopyFile_ShouldCopyAndReturnByteCount()
    {
        var source = Path.Combine(_testDir, "source.txt");
        var targetDir = Path.Combine(_testDir, "target");
        Directory.CreateDirectory(targetDir);
        var target = Path.Combine(targetDir, "dest.txt");
        File.WriteAllText(source, "copy me");
        IFileSystemGateway gateway = new LocalFileSystemGateway();

        var bytes = gateway.CopyFile(source, target);

        Assert.True(File.Exists(target));
        Assert.True(bytes > 0);
        Assert.Equal(File.ReadAllText(source), File.ReadAllText(target));
    }

    [Fact]
    public void CopyFile_ShouldCreateTargetSubdirectories()
    {
        var source = Path.Combine(_testDir, "source.txt");
        var target = Path.Combine(_testDir, "deep", "nested", "dest.txt");
        File.WriteAllText(source, "deep copy");
        IFileSystemGateway gateway = new LocalFileSystemGateway();

        var bytes = gateway.CopyFile(source, target);

        Assert.True(File.Exists(target));
        Assert.True(bytes > 0);
    }

    [Fact]
    public void Exists_WithExistingPath_ShouldReturnTrue()
    {
        var filePath = Path.Combine(_testDir, "exists.txt");
        File.WriteAllText(filePath, "I exist");
        IFileSystemGateway gateway = new LocalFileSystemGateway();

        Assert.True(gateway.Exists(filePath));
        Assert.True(gateway.Exists(_testDir));
    }

    [Fact]
    public void Exists_WithMissingPath_ShouldReturnFalse()
    {
        IFileSystemGateway gateway = new LocalFileSystemGateway();

        Assert.False(gateway.Exists(Path.Combine(_testDir, "nonexistent.txt")));
    }
}
