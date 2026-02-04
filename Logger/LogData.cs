

namespace Logger;

public class LogData
{

    public string Timestamp { get; set; } = string.Empty;

    public string BackupJobName { get; set; } = string.Empty;

    public string SourceFilePath { get; set; } = string.Empty;

    public string TargetFilePath { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public long TransferTimeMs { get; set; }
    public long EncryptionTimeMs { get; set; }
}