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

/// <summary>
/// Defines where logs should be written.
/// </summary>
public enum LogMode
{
    /// <summary>
    /// Logs are written only to the local machine.
    /// </summary>
    LocalOnly,

    /// <summary>
    /// Logs are sent only to the centralized Docker server.
    /// </summary>
    CentralizedOnly,

    /// <summary>
    /// Logs are written both locally and sent to the centralized server.
    /// </summary>
    LocalAndCentralized
}

