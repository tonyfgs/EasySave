using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs;
using Application.Ports;

namespace Infrastructure;

/// <summary>
/// Adapter for sending logs to the centralized Docker LogCentralizer service.
/// </summary>
public class LogCentralizationAdapter : ILogCentralizationService
{
    private readonly HttpClient _httpClient;
    private readonly string _serverUrl;
    private LogMode _logMode;
    private readonly JsonSerializerOptions _jsonOptions;

    public LogCentralizationAdapter(string serverUrl, LogMode initialMode = LogMode.LocalOnly)
    {
        _serverUrl = serverUrl.TrimEnd('/');
        _logMode = initialMode;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task SendLogAsync(TransferLog log, string userId)
    {
        if (_logMode == LogMode.LocalOnly)
        {
            return;
        }

        var entry = new
        {
            log.Timestamp,
            log.BackupName,
            log.SourcePath,
            log.DestPath,
            log.FileSize,
            log.TransferTimeMs,
            log.EncryptionTimeMs,
            UserId = userId,
            MachineName = Environment.MachineName
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{_serverUrl}/api/log",
                entry,
                _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[LogCentralization] ⚠ Server returned {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[LogCentralization] ⚠ Failed to send log: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("[LogCentralization] ⚠ Request timeout");
        }
    }

    public LogMode GetLogMode()
    {
        return _logMode;
    }

    public void SetLogMode(LogMode mode)
    {
        _logMode = mode;
        Console.WriteLine($"[LogCentralization] Mode changed to: {mode}");
    }

    public async Task<bool> IsServerAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_serverUrl}/api/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

