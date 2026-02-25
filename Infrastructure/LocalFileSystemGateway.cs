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

    public async Task<long> CopyFileAsync(string source, string target, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var normalizedTarget = Path.GetFullPath(target);
        var targetDir = Path.GetDirectoryName(normalizedTarget);
        if (!string.IsNullOrEmpty(targetDir))
            Directory.CreateDirectory(targetDir);
        var sourceSize = new FileInfo(source).Length;
        await using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
        await using var targetStream = new FileStream(normalizedTarget, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);
        // TODO EPIC-04: D-02 requires deleting partial file on cancellation/IOException
        await sourceStream.CopyToAsync(targetStream, 81920, ct);
        return sourceSize;
    }

    public bool Exists(string path)
    {
        return Directory.Exists(path) || File.Exists(path);
    }
}
