using Shared;

namespace Application.Ports;

/// <summary>
/// Service for centralizing logs to a remote Docker server.
/// </summary>
public interface ILogCentralizationService
{
    /// <summary>
    /// Sends a log entry to the centralized server.
    /// </summary>
    Task SendLogAsync(DTOs.TransferLog log, string userId);

    /// <summary>
    /// Gets the current log mode.
    /// </summary>
    LogMode GetLogMode();

    /// <summary>
    /// Sets the log mode.
    /// </summary>
    void SetLogMode(LogMode mode);

    /// <summary>
    /// Checks if the centralized server is available.
    /// </summary>
    Task<bool> IsServerAvailableAsync();
}
