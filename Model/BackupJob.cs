namespace Model;

public record BackupJob(
    long Id,
    string Name,
    string SourcePath,
    string DestinationPath,
    BackupType Type,
    DateTime LastBackupDate
);