namespace Model;

public interface IBackupStrategy
{
    IReadOnlyList<FileDescriptor> SelectFiles(BackupJob job, IReadOnlyList<FileDescriptor> files);
}
