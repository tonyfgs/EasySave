namespace Model;

public class BackupDomainService
{
    public void ValidateJobLimit(int currentCount, int maxJobs)
    {
        if (currentCount >= maxJobs)
            throw new JobLimitExceededException(maxJobs);
    }

    public IReadOnlyList<FileDescriptor> SelectFilesForBackup(
        BackupJob job,
        IReadOnlyList<FileDescriptor> files,
        IBackupStrategy strategy)
    {
        job.Validate();
        return strategy.SelectFiles(job, files);
    }
}
