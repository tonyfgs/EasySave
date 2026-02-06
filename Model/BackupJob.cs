using System.Text.Json.Serialization;

namespace Model;

public class BackupJob
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public BackupType Type { get; set; }
    public DateTime? LastFullBackupDate { get; set; }
    public DateTime CreatedDate { get; set; }

    public BackupJob(int id, string name, string sourcePath, string targetPath, BackupType type)
    {
        Id = id;
        Name = name;
        SourcePath = sourcePath;
        TargetPath = targetPath;
        Type = type;
        LastFullBackupDate = null;
        CreatedDate = DateTime.Now;
    }

    [JsonConstructor]
    public BackupJob() { }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidBackupJobException("Backup job name cannot be empty.");

        if (string.IsNullOrWhiteSpace(SourcePath))
            throw new InvalidBackupJobException("Source path cannot be empty.");

        if (string.IsNullOrWhiteSpace(TargetPath))
            throw new InvalidBackupJobException("Target path cannot be empty.");

        if (string.Equals(SourcePath, TargetPath, StringComparison.Ordinal))
            throw new InvalidBackupJobException("Source and target paths cannot be the same.");
    }

    public bool IsFileModifiedSinceLastFull(FileDescriptor file)
    {
        if (LastFullBackupDate is null)
            return true;

        return file.LastModified > LastFullBackupDate.Value;
    }

    public void MarkFullBackupCompleted(DateTime date)
    {
        LastFullBackupDate = date;
    }
}
