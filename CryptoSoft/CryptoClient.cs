using System.Net.Sockets;
using System.Text;

namespace CryptoSoft;

/// <summary>
/// TCP client to communicate with the CryptoSoft server (cross-platform).
/// </summary>
public class CryptoClient
{
    public const int DefaultPort = 19283;

    private readonly int _port;
    private readonly int _timeoutMs;

    public CryptoClient(int port = DefaultPort, int timeoutMs = 300000)
    {
        _port = port;
        _timeoutMs = timeoutMs;
    }

    public record CryptoResponse(bool Success, int ExitCode, long DurationMs, string? ErrorMessage);

    public CryptoResponse Encrypt(string filePath, string key)
    {
        return SendRequest("encrypt", filePath, key);
    }

    public CryptoResponse Decrypt(string filePath, string key)
    {
        return SendRequest("decrypt", filePath, key);
    }

    private CryptoResponse SendRequest(string operation, string filePath, string key)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var client = new TcpClient();

            // Connect to server with timeout
            var connectTask = client.ConnectAsync(System.Net.IPAddress.Loopback, _port);
            if (!connectTask.Wait(_timeoutMs))
            {
                return new CryptoResponse(false, 6, stopwatch.ElapsedMilliseconds,
                    "Timeout: unable to connect to CryptoSoft server");
            }

            using var stream = client.GetStream();
            stream.ReadTimeout = _timeoutMs;
            stream.WriteTimeout = _timeoutMs;

            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
            using var reader = new StreamReader(stream, Encoding.UTF8);

            // Send the request
            writer.WriteLine($"{operation}|{filePath}|{key}");

            // Read the response
            var response = reader.ReadLine();
            stopwatch.Stop();

            if (string.IsNullOrEmpty(response))
            {
                return new CryptoResponse(false, 3, stopwatch.ElapsedMilliseconds,
                    "No response from server");
            }

            // Parse the response (format: "OK|0|durationMs" or "ERROR|code|message")
            var parts = response.Split('|');
            if (parts.Length < 3)
            {
                return new CryptoResponse(false, 3, stopwatch.ElapsedMilliseconds,
                    "Invalid response from server");
            }

            var status = parts[0];
            var code = int.TryParse(parts[1], out var c) ? c : 3;

            if (status == "OK")
            {
                var serverDuration = long.TryParse(parts[2], out var d) ? d : stopwatch.ElapsedMilliseconds;
                return new CryptoResponse(true, 0, serverDuration, null);
            }
            else
            {
                return new CryptoResponse(false, code, stopwatch.ElapsedMilliseconds, parts[2]);
            }
        }
        catch (SocketException ex)
        {
            return new CryptoResponse(false, 6, stopwatch.ElapsedMilliseconds,
                $"Connection error: {ex.Message}");
        }
        catch (TimeoutException)
        {
            return new CryptoResponse(false, 6, stopwatch.ElapsedMilliseconds,
                "Timeout: unable to connect to CryptoSoft server");
        }
        catch (IOException ex)
        {
            return new CryptoResponse(false, 3, stopwatch.ElapsedMilliseconds,
                $"I/O error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new CryptoResponse(false, 3, stopwatch.ElapsedMilliseconds,
                $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if the server is running.
    /// </summary>
    public static bool IsServerRunning(int port = DefaultPort)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(System.Net.IPAddress.Loopback, port);
            if (connectTask.Wait(500))
            {
                return client.Connected;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}
