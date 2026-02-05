using Application.Ports;
using Model;

namespace Infrastructure;

public class LocalFileSystemGateway : IFileSystemGateway
{
    public List<FileDescriptor> EnumerateFiles(string path)
    {
        if (!Directory.Exists(path))
            return new List<FileDescriptor>();

        return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
            .Select(f =>
            {
                var info = new FileInfo(f);
                return new FileDescriptor(f, info.Length, info.LastWriteTime);
            })
            .ToList();
    }

    public void EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public long CopyFile(string source, string target)
    {
        var targetDir = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(targetDir))
            Directory.CreateDirectory(targetDir);

        File.Copy(source, target, overwrite: true);
        return new FileInfo(target).Length;
    }

    public bool Exists(string path)
    {
        return Directory.Exists(path) || File.Exists(path);
    }
}
