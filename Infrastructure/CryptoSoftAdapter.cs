using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using Application.Ports;

namespace Infrastructure;

public class CryptoSoftAdapter : IEncryptionService
{
    private readonly IEncryptionConfig _config;
    private readonly string _cryptoSoftPath;
    private readonly int _timeoutMs;
    private readonly int _maxRetries;
    private readonly int _initialRetryDelayMs;
    private readonly int _port;

    private static readonly object _serverStartLock = new();
    private static Process? _serverProcess;

    public CryptoSoftAdapter(
        IEncryptionConfig config,
        string cryptoSoftPath,
        int timeoutMs = 300000,
        int maxRetries = 5,
        int initialRetryDelayMs = 100,
        int port = 19283)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _cryptoSoftPath = cryptoSoftPath ?? throw new ArgumentNullException(nameof(cryptoSoftPath));
        _timeoutMs = timeoutMs;
        _maxRetries = maxRetries;
        _initialRetryDelayMs = initialRetryDelayMs;
        _port = port;
    }

    public CryptoResult EncryptFile(string filePath)
    {
        return ExecuteAsync("encrypt", filePath).GetAwaiter().GetResult();
    }

    public CryptoResult DecryptFile(string filePath)
    {
        return ExecuteAsync("decrypt", filePath).GetAwaiter().GetResult();
    }

    private async Task<CryptoResult> ExecuteAsync(string operation, string filePath)
    {
        var key = _config.GetEncryptionKey();

        // If key is empty -> no encryption, immediate success
        if (string.IsNullOrWhiteSpace(key))
        {
            return new CryptoResult
            {
                Success = true,
                DurationMs = 0,
                ErrorCode = CryptoErrorCode.None,
                ErrorMessage = null
            };
        }

        // Try to use the TCP server, otherwise fallback to standalone
        var serverRunning = await EnsureServerRunningAsync();

        if (serverRunning)
        {
            return await ExecuteViaTcpAsync(operation, filePath, key);
        }
        else
        {
            // Fallback: execute in standalone mode via Process
            return await ExecuteStandaloneAsync(operation, filePath, key);
        }
    }

    private async Task<bool> EnsureServerRunningAsync()
    {
        if (IsServerRunning())
        {
            return true;
        }

        // Try to start the server
        lock (_serverStartLock)
        {
            // Double-check after lock
            if (IsServerRunning())
            {
                return true;
            }

            try
            {
                if (!File.Exists(_cryptoSoftPath))
                {
                    return false;
                }

                _serverProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _cryptoSoftPath,
                        Arguments = "server",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false
                    }
                };

                _serverProcess.Start();
            }
            catch
            {
                return false;
            }
        }

        // Wait for server to be ready (max 5 seconds)
        for (int i = 0; i < 50; i++)
        {
            await Task.Delay(100);
            if (IsServerRunning())
            {
                return true;
            }
        }

        return false;
    }

    private bool IsServerRunning()
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(System.Net.IPAddress.Loopback, _port);
            return connectTask.Wait(500) && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Executes the operation via the TCP server with async retry.
    /// </summary>
    private async Task<CryptoResult> ExecuteViaTcpAsync(string operation, string filePath, string key)
    {
        var totalStopwatch = Stopwatch.StartNew();
        int retryCount = 0;
        int currentDelayMs = _initialRetryDelayMs;

        while (true)
        {
            var result = await SendTcpRequestAsync(operation, filePath, key);

            // If success or error other than connection issue, return immediately
            if (result.Success || result.ErrorCode != CryptoErrorCode.AlreadyRunning)
            {
                return result;
            }

            // Check if max retries reached
            if (retryCount >= _maxRetries)
            {
                return new CryptoResult
                {
                    Success = false,
                    DurationMs = totalStopwatch.ElapsedMilliseconds,
                    ErrorCode = CryptoErrorCode.AlreadyRunning,
                    ErrorMessage = $"Unable to connect to server after {_maxRetries} attempts"
                };
            }

            // Check if total timeout exceeded
            if (totalStopwatch.ElapsedMilliseconds + currentDelayMs > _timeoutMs)
            {
                return new CryptoResult
                {
                    Success = false,
                    DurationMs = totalStopwatch.ElapsedMilliseconds,
                    ErrorCode = CryptoErrorCode.Timeout,
                    ErrorMessage = "Timeout reached waiting for CryptoSoft server"
                };
            }

            // Exponential backoff with Task.Delay (non-blocking)
            await Task.Delay(currentDelayMs);
            currentDelayMs = Math.Min(currentDelayMs * 2, 5000);
            retryCount++;
        }
    }

    private async Task<CryptoResult> SendTcpRequestAsync(string operation, string filePath, string key)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var client = new TcpClient();

            var connectTask = client.ConnectAsync(System.Net.IPAddress.Loopback, _port);
            if (!await Task.Run(() => connectTask.Wait(_timeoutMs)))
            {
                return new CryptoResult
                {
                    Success = false,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    ErrorCode = CryptoErrorCode.AlreadyRunning,
                    ErrorMessage = "Server connection timeout"
                };
            }

            using var stream = client.GetStream();
            stream.ReadTimeout = _timeoutMs;
            stream.WriteTimeout = _timeoutMs;

            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
            using var reader = new StreamReader(stream, Encoding.UTF8);

            // Send the request
            await writer.WriteLineAsync($"{operation}|{filePath}|{key}");

            // Read the response
            var response = await reader.ReadLineAsync();
            stopwatch.Stop();

            if (string.IsNullOrEmpty(response))
            {
                return new CryptoResult
                {
                    Success = false,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    ErrorCode = CryptoErrorCode.IoError,
                    ErrorMessage = "No response from server"
                };
            }

            // Parse the response (format: "OK|0|durationMs" or "ERROR|code|message")
            var parts = response.Split('|', 3);
            if (parts.Length < 3)
            {
                return new CryptoResult
                {
                    Success = false,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    ErrorCode = CryptoErrorCode.IoError,
                    ErrorMessage = "Invalid response from server"
                };
            }

            var status = parts[0];
            var code = int.TryParse(parts[1], out var c) ? c : 3;

            if (status == "OK")
            {
                var serverDuration = long.TryParse(parts[2], out var d) ? d : stopwatch.ElapsedMilliseconds;
                return new CryptoResult
                {
                    Success = true,
                    DurationMs = serverDuration,
                    ErrorCode = CryptoErrorCode.None,
                    ErrorMessage = null
                };
            }
            else
            {
                return new CryptoResult
                {
                    Success = false,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    ErrorCode = MapExitCode(code),
                    ErrorMessage = parts[2]
                };
            }
        }
        catch (SocketException ex)
        {
            return new CryptoResult
            {
                Success = false,
                DurationMs = stopwatch.ElapsedMilliseconds,
                ErrorCode = CryptoErrorCode.AlreadyRunning,
                ErrorMessage = $"Connection error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new CryptoResult
            {
                Success = false,
                DurationMs = stopwatch.ElapsedMilliseconds,
                ErrorCode = CryptoErrorCode.IoError,
                ErrorMessage = $"TCP error: {ex.Message}"
            };
        }
    }

    private async Task<CryptoResult> ExecuteStandaloneAsync(string operation, string filePath, string key)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = _cryptoSoftPath,
                Arguments = $"{operation} \"{filePath}\" \"{key}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            process.Start();

            // Wait asynchronously
            var completedTask = await Task.Run(() => process.WaitForExit(_timeoutMs));
            stopwatch.Stop();

            if (!completedTask)
            {
                try { process.Kill(); }
                catch { /* Ignore */ }

                return new CryptoResult
                {
                    Success = false,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    ErrorCode = CryptoErrorCode.Timeout,
                    ErrorMessage = $"CryptoSoft timeout after {_timeoutMs}ms"
                };
            }

            var exitCode = process.ExitCode;
            var errorCode = MapExitCode(exitCode);

            if (exitCode == 0)
            {
                return new CryptoResult
                {
                    Success = true,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    ErrorCode = CryptoErrorCode.None,
                    ErrorMessage = null
                };
            }

            var errorOutput = await process.StandardError.ReadToEndAsync();
            return new CryptoResult
            {
                Success = false,
                DurationMs = stopwatch.ElapsedMilliseconds,
                ErrorCode = errorCode,
                ErrorMessage = string.IsNullOrWhiteSpace(errorOutput)
                    ? $"CryptoSoft {operation} failed with code {exitCode}"
                    : errorOutput.Trim()
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new CryptoResult
            {
                Success = false,
                DurationMs = stopwatch.ElapsedMilliseconds,
                ErrorCode = CryptoErrorCode.Unknown,
                ErrorMessage = $"Error during CryptoSoft execution: {ex.Message}"
            };
        }
    }

    private static CryptoErrorCode MapExitCode(int exitCode)
    {
        return exitCode switch
        {
            0 => CryptoErrorCode.None,
            1 => CryptoErrorCode.FileNotFound,
            2 => CryptoErrorCode.InvalidArguments,
            3 => CryptoErrorCode.IoError,
            4 => CryptoErrorCode.AuthTagInvalid,
            5 => CryptoErrorCode.InvalidKey,
            6 => CryptoErrorCode.AlreadyRunning,
            _ => CryptoErrorCode.Unknown
        };
    }
}
