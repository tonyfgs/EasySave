namespace Model;

public record BackupJob(
    string Name,
    string SourcePath,
    string DestinationPath,
    BackupType Type,
    DateTime LastBackupDate
);