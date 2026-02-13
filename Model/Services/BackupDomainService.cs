namespace Model;

public class BackupDomainService
{
    [Obsolete("Job limit removed in v2.0 (FR-02, issue #10). Retained per PRD decision.")]
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
