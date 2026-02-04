

namespace Logger;

public class LogData
{

    public string Timestamp { get; set; }

    public string BackupJobName { get; set; }

    public string SourceFilePath { get; set; }

    public string TargetFilePath { get; set; }

    public long FileSize { get; set; }

    public long TransferTimeMs { get; set; }
    public long EncryptionTimeMs { get; set; }
}