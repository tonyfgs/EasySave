namespace Model;

public class FullBackupStrategy : IBackupStrategy
{
    public IReadOnlyList<FileDescriptor> SelectFiles(BackupJob job, IReadOnlyList<FileDescriptor> files)
    {
        return files;
    }
}
