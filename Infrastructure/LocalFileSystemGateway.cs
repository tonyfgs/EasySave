using Application.Ports;
using Model;

namespace Infrastructure;

public class LocalFileSystemGateway : IFileSystemGateway
{
    public List<FileDescriptor> EnumerateFiles(string path)
    {
        var normalizedPath = Path.GetFullPath(path);
        if (!Directory.Exists(normalizedPath))
            return new List<FileDescriptor>();

        return Directory.EnumerateFiles(normalizedPath, "*", SearchOption.AllDirectories)
            .Select(f =>
            {
                var fullPath = Path.GetFullPath(f);
                var info = new FileInfo(fullPath);
                return new FileDescriptor(fullPath, info.Length, info.LastWriteTime);
            })
            .ToList();
    }

    public void EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public long CopyFile(string source, string target)
    {
        var normalizedTarget = Path.GetFullPath(target);
        var targetDir = Path.GetDirectoryName(normalizedTarget);
        if (!string.IsNullOrEmpty(targetDir))
            Directory.CreateDirectory(targetDir);

        var sourceSize = new FileInfo(source).Length;
        File.Copy(source, normalizedTarget, overwrite: true);
        return sourceSize;
    }

    public bool Exists(string path)
    {
        return Directory.Exists(path) || File.Exists(path);
    }
}
