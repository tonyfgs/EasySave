using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs;
using Application.Ports;
using Microsoft.Extensions.Logging;
using Shared;

namespace Infrastructure;

/// <summary>
/// Adapter for sending logs to the centralized Docker LogCentralizer service.
/// Uses static HttpClient to avoid socket exhaustion.
/// </summary>
public class LogCentralizationAdapter : ILogCentralizationService
{
    // Static HttpClient to avoid socket exhaustion (recommended .NET pattern)
    private static readonly HttpClient SharedHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private readonly string _serverUrl;
    private readonly ILogger<LogCentralizationAdapter>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private LogMode _logMode;

    public LogCentralizationAdapter(
        string serverUrl,
        LogMode initialMode = LogMode.LocalOnly,
        ILogger<LogCentralizationAdapter>? logger = null)
    {
        _serverUrl = serverUrl.TrimEnd('/');
        _logMode = initialMode;
        _logger = logger;
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
            Timestamp = log.Timestamp.Kind == DateTimeKind.Utc
                ? log.Timestamp
                : log.Timestamp.ToUniversalTime(),
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
            var response = await SharedHttpClient.PostAsJsonAsync(
                $"{_serverUrl}/api/log",
                entry,
                _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning(
                    "LogCentralization server returned {StatusCode}",
                    response.StatusCode);
            }
            else
            {
                _logger?.LogDebug(
                    "Log sent successfully for backup {BackupName}",
                    log.BackupName);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(
                ex,
                "Failed to send log to centralized server: {Message}",
                ex.Message);
        }
        catch (TaskCanceledException)
        {
            _logger?.LogWarning("LogCentralization request timeout");
        }
    }

    public LogMode GetLogMode()
    {
        return _logMode;
    }

    public void SetLogMode(LogMode mode)
    {
        _logMode = mode;
        _logger?.LogInformation("Log mode changed to: {Mode}", mode);
    }

    public async Task<bool> IsServerAvailableAsync()
    {
        try
        {
            var response = await SharedHttpClient.GetAsync($"{_serverUrl}/api/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Server availability check failed");
            return false;
        }
    }
}
