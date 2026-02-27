using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using Application.Ports;

namespace Infrastructure;

public class CryptoSoftAdapter : IEncryptionService, IDisposable
{
    private readonly IEncryptionConfig _config;
    private readonly string _cryptoSoftPath;
    private readonly int _timeoutMs;
    private readonly int _maxRetries;
    private readonly int _initialRetryDelayMs;
    private readonly int _port;
    private readonly string _serverArguments;

    private readonly object _serverStartLock = new();
    private Process? _serverProcess;
    private readonly StderrRingBuffer _stderrBuffer = new(200);
    private DataReceivedEventHandler? _stdoutHandler;
    private DataReceivedEventHandler? _stderrHandler;
    private bool _disposed;

    public CryptoSoftAdapter(
        IEncryptionConfig config,
        string cryptoSoftPath,
        int timeoutMs = 300000,
        int maxRetries = 5,
        int initialRetryDelayMs = 100,
        int port = 19283,
        string serverArguments = "server")
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _cryptoSoftPath = cryptoSoftPath ?? throw new ArgumentNullException(nameof(cryptoSoftPath));
        _timeoutMs = timeoutMs;
        _maxRetries = maxRetries;
        _initialRetryDelayMs = initialRetryDelayMs;
        _port = port;
        _serverArguments = serverArguments;
    }

    public CryptoResult EncryptFile(string filePath)
    {
        return ExecuteAsync("encrypt", filePath).GetAwaiter().GetResult();
    }

    public CryptoResult DecryptFile(string filePath)
    {
        return ExecuteAsync("decrypt", filePath).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Returns the last stderr lines from the most recent server startup attempt.
    /// Useful for diagnosing why the server failed to start or connect.
    /// </summary>
    public IReadOnlyList<string> GetServerStderrLines() => _stderrBuffer.GetLines();

    /// <summary>
    /// Stops the server process and cleans up handlers.
    /// Does not dispose the adapter itself, allowing it to be restarted.
    /// </summary>
    public void StopServer()
    {
        lock (_serverStartLock)
        {
            UnsubscribeHandlers();

            if (_serverProcess is not null)
            {
                if (!_serverProcess.HasExited)
                {
                    try { _serverProcess.Kill(entireProcessTree: true); }
                    catch { /* Process may have already exited */ }

                    try { _serverProcess.WaitForExit(3000); }
                    catch { /* Timeout is acceptable */ }
                }

                _serverProcess.Dispose();
                _serverProcess = null;
            }

            _stderrBuffer.Clear();
        }
    }

    public void Dispose()
    {
        lock (_serverStartLock)
        {
            if (_disposed) return;
            _disposed = true;

            UnsubscribeHandlers();

            if (_serverProcess is not null)
            {
                if (!_serverProcess.HasExited)
                {
                    try { _serverProcess.Kill(entireProcessTree: true); }
                    catch { /* Process may have already exited */ }

                    try { _serverProcess.WaitForExit(3000); }
                    catch { /* Timeout is acceptable */ }
                }

                _serverProcess.Dispose();
                _serverProcess = null;
            }

            _stderrBuffer.Clear();
        }
    }

    private async Task<CryptoResult> ExecuteAsync(string operation, string filePath)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CryptoSoftAdapter));

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
            if (_disposed) return false;

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

                // Clear buffer for this startup attempt
                _stderrBuffer.Clear();

                // Unsubscribe old handlers if any (handles restart case)
                UnsubscribeHandlers();

                _serverProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _cryptoSoftPath,
                        Arguments = _serverArguments,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                // Create and subscribe handler delegates for async draining
                _stdoutHandler = (sender, e) =>
                {
                    // Drain stdout to prevent OS pipe buffer from filling.
                    // We intentionally discard the content.
                };
                _stderrHandler = (sender, e) =>
                {
                    if (e.Data is not null)
                        _stderrBuffer.Append(e.Data);
                };

                _serverProcess.OutputDataReceived += _stdoutHandler;
                _serverProcess.ErrorDataReceived += _stderrHandler;

                _serverProcess.Start();

                // Begin async reading IMMEDIATELY after Start() to prevent deadlocks.
                // These callbacks drain the OS pipe buffers as data arrives.
                try
                {
                    _serverProcess.BeginOutputReadLine();
                    _serverProcess.BeginErrorReadLine();
                }
                catch
                {
                    try { _serverProcess.Kill(entireProcessTree: true); } catch { }
                    try { _serverProcess.WaitForExit(3000); } catch { }
                    _serverProcess.Dispose();
                    _serverProcess = null;
                    _stdoutHandler = null;
                    _stderrHandler = null;
                    return false;
                }
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

    private void UnsubscribeHandlers()
    {
        if (_serverProcess is not null)
        {
            if (_stdoutHandler is not null)
                _serverProcess.OutputDataReceived -= _stdoutHandler;
            if (_stderrHandler is not null)
                _serverProcess.ErrorDataReceived -= _stderrHandler;
        }
        _stdoutHandler = null;
        _stderrHandler = null;
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

    /// <summary>
    /// Fallback: executes CryptoSoft as a short-lived standalone process.
    /// Uses ReadToEndAsync on stderr which is acceptable for short-lived processes
    /// (the "no synchronous read" constraint targets the long-lived server process).
    /// </summary>
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
