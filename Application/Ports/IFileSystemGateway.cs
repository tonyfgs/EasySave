using Model;

namespace Application.Ports;

public interface IFileSystemGateway
{
    List<FileDescriptor> EnumerateFiles(string path);
    void EnsureDirectory(string path);
    long CopyFile(string source, string target);
    Task<long> CopyFileAsync(string source, string target, CancellationToken ct = default);
    bool Exists(string path);
}
