namespace Shared;

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

