using Model;

namespace ModelTest;

public class FileDescriptorTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var date = new DateTime(2025, 1, 15, 10, 30, 0);
        var fd = new FileDescriptor("/path/to/file.txt", 1024, date);

        Assert.Equal("/path/to/file.txt", fd.Path);
        Assert.Equal(1024, fd.Size);
        Assert.Equal(date, fd.LastModified);
    }

    [Fact]
    public void Equals_SameValues_ShouldBeEqual()
    {
        var date = new DateTime(2025, 1, 15);
        var fd1 = new FileDescriptor("/path/file.txt", 100, date);
        var fd2 = new FileDescriptor("/path/file.txt", 100, date);

        Assert.Equal(fd1, fd2);
        Assert.True(fd1.Equals(fd2));
    }

    [Fact]
    public void Equals_DifferentValues_ShouldNotBeEqual()
    {
        var date = new DateTime(2025, 1, 15);
        var fd1 = new FileDescriptor("/path/file1.txt", 100, date);
        var fd2 = new FileDescriptor("/path/file2.txt", 100, date);

        Assert.NotEqual(fd1, fd2);
    }

    [Fact]
    public void GetHashCode_SameValues_ShouldBeSame()
    {
        var date = new DateTime(2025, 1, 15);
        var fd1 = new FileDescriptor("/path/file.txt", 100, date);
        var fd2 = new FileDescriptor("/path/file.txt", 100, date);

        Assert.Equal(fd1.GetHashCode(), fd2.GetHashCode());
    }

    [Fact]
    public void FileDescriptor_ShouldBeImmutable_InHashSet()
    {
        var date = new DateTime(2025, 1, 15);
        var set = new HashSet<FileDescriptor>
        {
            new("/path/file.txt", 100, date),
            new("/path/file.txt", 100, date)
        };

        Assert.Single(set);
    }
}
