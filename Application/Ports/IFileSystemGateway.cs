using Model;

namespace Application.Ports;

public interface IFileSystemGateway
{
    List<FileDescriptor> EnumerateFiles(string path);
    void EnsureDirectory(string path);
    long CopyFile(string source, string target);
    bool Exists(string path);
}
