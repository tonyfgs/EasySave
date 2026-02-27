using System.Text.Json;
using System.Xml.Serialization;
using LogCentralizer.Models;

namespace LogCentralizer.Services;

public class LogAggregator
{
    private readonly string _logDirectory;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<LogAggregator> _logger;

    public LogAggregator(IConfiguration configuration, ILogger<LogAggregator> logger)
    {
        _logger = logger;
        _logDirectory = configuration.GetValue<string>("LogDirectory") ?? "/app/logs";
        Directory.CreateDirectory(_logDirectory);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _logger.LogInformation("LogAggregator initialized. Log directory: {LogDir}", _logDirectory);
    }

    /// <summary>
    /// Adds a log entry to today's log file using append (O(1)).
    /// Uses JSON Lines format (one JSON object per line).
    /// </summary>
    public async Task AddLogAsync(LogEntry entry)
    {
        await _writeLock.WaitAsync();
        try
        {
            var filePath = GetLogFilePath(DateOnly.FromDateTime(entry.Timestamp));

            // Append single JSON line (O(1) write instead of O(n) rewrite)
            var jsonLine = JsonSerializer.Serialize(entry, _jsonOptions);
            await File.AppendAllTextAsync(filePath, jsonLine + Environment.NewLine);

            _logger.LogDebug(
                "Log received from {MachineName}/{UserId}: {BackupName}",
                entry.MachineName,
                entry.UserId,
                entry.BackupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write log entry: {Message}", ex.Message);
            throw;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Gets all log entries for a specific date.
    /// Reads JSON Lines format (one JSON object per line).
    /// </summary>
    public async Task<List<LogEntry>> GetLogsAsync(DateOnly date)
    {
        var filePath = GetLogFilePath(date);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"No logs for {date}");
        }

        return await LoadEntriesFromJsonLinesAsync(filePath);
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
                var entries = await LoadEntriesFromJsonLinesAsync(file);
                stats.TotalEntries += entries.Count;
                stats.TotalFilesTransferred += entries.Count;
                stats.TotalBytesTransferred += entries.Sum(e => e.FileSize);

                foreach (var entry in entries)
                {
                    if (!string.IsNullOrEmpty(entry.UserId) && !stats.UniqueUsers.Contains(entry.UserId))
                        stats.UniqueUsers.Add(entry.UserId);

                    if (!string.IsNullOrEmpty(entry.MachineName) && !stats.UniqueMachines.Contains(entry.MachineName))
                        stats.UniqueMachines.Add(entry.MachineName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read log file {File}, skipping", file);
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

    /// <summary>
    /// Loads entries from JSON Lines format (one JSON object per line).
    /// </summary>
    private async Task<List<LogEntry>> LoadEntriesFromJsonLinesAsync(string filePath)
    {
        var entries = new List<LogEntry>();

        if (!File.Exists(filePath))
        {
            return entries;
        }

        var lines = await File.ReadAllLinesAsync(filePath);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var entry = JsonSerializer.Deserialize<LogEntry>(line, _jsonOptions);
                if (entry != null)
                {
                    entries.Add(entry);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse log line, skipping: {Line}", line);
            }
        }

        return entries;
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

