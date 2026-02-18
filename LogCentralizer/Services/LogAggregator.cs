using System.Text.Json;
using System.Xml.Serialization;

namespace LogCentralizer.Services;

/// <summary>
/// Aggregates logs from multiple EasySave clients into daily log files.
/// </summary>
public class LogAggregator
{
    private readonly string _logDirectory;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;

    public LogAggregator(IConfiguration configuration)
    {
        _logDirectory = configuration.GetValue<string>("LogDirectory") ?? "/app/logs";
        Directory.CreateDirectory(_logDirectory);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Adds a log entry to today's log file.
    /// </summary>
    public async Task AddLogAsync(LogEntry entry)
    {
        await _writeLock.WaitAsync();
        try
        {
            var filePath = GetLogFilePath(DateOnly.FromDateTime(entry.Timestamp));
            var entries = await LoadEntriesAsync(filePath);

            entries.Add(entry);

            var json = JsonSerializer.Serialize(entries, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);

            Console.WriteLine($"[LogAggregator] ✓ Log from {entry.MachineName}/{entry.UserId}: {entry.BackupName}");
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Gets all log entries for a specific date.
    /// </summary>
    public async Task<List<LogEntry>> GetLogsAsync(DateOnly date)
    {
        var filePath = GetLogFilePath(date);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"No logs for {date}");
        }

        return await LoadEntriesAsync(filePath);
    }

    /// <summary>
    /// Gets statistics about the logs.
    /// </summary>
    public async Task<LogStatistics> GetStatisticsAsync()
    {
        var stats = new LogStatistics();
        var files = Directory.GetFiles(_logDirectory, "*.json");

        foreach (var file in files)
        {
            try
            {
                var entries = await LoadEntriesAsync(file);
                stats.TotalEntries += entries.Count;
                stats.TotalFilesTransferred += entries.Count;
                stats.TotalBytesTransferred += entries.Sum(e => e.FileSize);

                foreach (var entry in entries)
                {
                    if (!stats.UniqueUsers.Contains(entry.UserId))
                        stats.UniqueUsers.Add(entry.UserId);

                    if (!stats.UniqueMachines.Contains(entry.MachineName))
                        stats.UniqueMachines.Add(entry.MachineName);
                }
            }
            catch
            {
                // Skip corrupted files
            }
        }

        stats.LogFilesCount = files.Length;

        if (files.Length > 0)
        {
            var sortedFiles = files.OrderBy(f => f).ToArray();
            var oldestFileName = Path.GetFileNameWithoutExtension(sortedFiles.First());
            var newestFileName = Path.GetFileNameWithoutExtension(sortedFiles.Last());

            if (DateOnly.TryParse(oldestFileName, out var oldest))
                stats.OldestLog = oldest;
            if (DateOnly.TryParse(newestFileName, out var newest))
                stats.NewestLog = newest;
        }

        return stats;
    }

    /// <summary>
    /// Serializes log entries to XML format.
    /// </summary>
    public string SerializeToXml(List<LogEntry> entries)
    {
        var wrapper = new LogEntriesWrapper { Entries = entries };
        var serializer = new XmlSerializer(typeof(LogEntriesWrapper));

        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, wrapper);
        return stringWriter.ToString();
    }

    private string GetLogFilePath(DateOnly date)
    {
        return Path.Combine(_logDirectory, $"{date:yyyy-MM-dd}.json");
    }

    private async Task<List<LogEntry>> LoadEntriesAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new List<LogEntry>();
        }

        var json = await File.ReadAllTextAsync(filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<LogEntry>();
        }

        return JsonSerializer.Deserialize<List<LogEntry>>(json, _jsonOptions)
               ?? new List<LogEntry>();
    }
}

/// <summary>
/// Statistics about the centralized logs.
/// </summary>
public class LogStatistics
{
    public int TotalEntries { get; set; }
    public int TotalFilesTransferred { get; set; }
    public long TotalBytesTransferred { get; set; }
    public int LogFilesCount { get; set; }
    public List<string> UniqueUsers { get; set; } = new();
    public List<string> UniqueMachines { get; set; } = new();
    public DateOnly? OldestLog { get; set; }
    public DateOnly? NewestLog { get; set; }
}

/// <summary>
/// Wrapper for XML serialization.
/// </summary>
[XmlRoot("Logs")]
public class LogEntriesWrapper
{
    [XmlElement("LogEntry")]
    public List<LogEntry> Entries { get; set; } = new();
}

