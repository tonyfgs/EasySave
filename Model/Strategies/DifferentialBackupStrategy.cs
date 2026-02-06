namespace Model;

public class DifferentialBackupStrategy : IBackupStrategy
{
    public IReadOnlyList<FileDescriptor> SelectFiles(BackupJob job, IReadOnlyList<FileDescriptor> files)
    {
        return files.Where(job.IsFileModifiedSinceLastFull).ToList().AsReadOnly();
    }
}
