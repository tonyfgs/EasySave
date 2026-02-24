using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CryptoSoft;

/// <summary>
/// CryptoSoft single-instance server using TCP localhost (cross-platform).
/// Accepts multiple simultaneous connections but processes encryptions one at a time.
/// </summary>
public class CryptoServer
{
    public const int DefaultPort = 19283;
    private const string MutexName = "Global\\CryptoSoftServerMutex";
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, Task> _activeOperations = new();

    // SemaphoreSlim to guarantee one encryption at a time (single-instance)
    private readonly SemaphoreSlim _encryptionSemaphore = new(1, 1);

    private readonly int _port;

    public CryptoServer(int port = DefaultPort)
    {
        _port = port;
    }

    /// <summary>
    /// Checks if the server is running via the TCP port.
    /// </summary>
    public static bool IsServerRunning(int port = DefaultPort)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(IPAddress.Loopback, port);
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

    public int Run()
    {
        // Use a Mutex to prevent multiple servers on the same machine (Windows only)
        Mutex? mutex = null;
        bool createdNew = false;

        if (OperatingSystem.IsWindows())
        {
            mutex = new Mutex(true, MutexName, out createdNew);
            if (!createdNew)
            {
                Console.Error.WriteLine("CryptoSoft Server is already running.");
                return 6;
            }
        }

        TcpListener? listener = null;
        try
        {
            listener = new TcpListener(IPAddress.Loopback, _port);
            listener.Start();

            Console.WriteLine("CryptoSoft Server started");
            Console.WriteLine($"   Port: {_port} (TCP localhost)");
            Console.WriteLine("   Cross-platform: Windows, Linux, macOS");
            Console.WriteLine("   Single-instance: Encryptions are processed one at a time");
            Console.WriteLine("   Waiting for connections...");
            Console.WriteLine("   Press Ctrl+C to stop.");
            Console.WriteLine();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                _cts.Cancel();
                Console.WriteLine("\nShutdown requested...");
            };

            // Accept multiple connections in parallel
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    // Polling to be able to react to cancellation
                    if (listener.Pending())
                    {
                        var client = listener.AcceptTcpClient();
                        // Store the actual task for clean shutdown
                        var operationId = Guid.NewGuid().ToString();
                        var task = Task.Run(() => HandleClientAsync(client, operationId), _cts.Token);
                        _activeOperations[operationId] = task;
                    }
                    else
                    {
                        Thread.Sleep(50); // Polling interval
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException)
                {
                    break;
                }
            }
        }
        catch (SocketException ex)
        {
            Console.Error.WriteLine($"Error: Port {_port} already in use or unavailable.");
            Console.Error.WriteLine($"Details: {ex.Message}");
            return 3;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Server error: {ex.Message}");
            return 3;
        }
        finally
        {
            listener?.Stop();

            // Wait for all ongoing operations to complete
            var tasks = _activeOperations.Values.ToArray();
            if (tasks.Length > 0)
            {
                Console.WriteLine($"Waiting for {tasks.Length} ongoing operation(s)...");
                Task.WaitAll(tasks, TimeSpan.FromSeconds(30));
            }

            _encryptionSemaphore.Dispose();

            if (OperatingSystem.IsWindows() && mutex != null)
            {
                mutex.ReleaseMutex();
                mutex.Dispose();
            }
        }

        Console.WriteLine("CryptoSoft Server stopped cleanly.");
        return 0;
    }

    private async Task HandleClientAsync(TcpClient tcpClient, string operationId)
    {
        try
        {
            using (tcpClient)
            await using (var stream = tcpClient.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            await using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                stream.ReadTimeout = 300000; // 5 minutes
                stream.WriteTimeout = 300000;

                // Read the request (format: "operation|filePath|key")
                var request = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(request))
                {
                    await writer.WriteLineAsync("ERROR|2|Empty request");
                    return;
                }

                var parts = request.Split('|');
                if (parts.Length < 3)
                {
                    await writer.WriteLineAsync("ERROR|2|Invalid format. Expected: operation|filePath|key");
                    return;
                }

                var operation = parts[0].ToLowerInvariant();
                var filePath = parts[1];
                var key = parts[2];

                Console.WriteLine($"Request received: {operation} {Path.GetFileName(filePath)}");

                // Wait for semaphore to guarantee single-instance encryption
                Console.WriteLine("   Waiting for single-instance lock...");
                await _encryptionSemaphore.WaitAsync(_cts.Token);

                try
                {
                    Console.WriteLine("   Lock acquired, processing...");
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    int exitCode;

                    if (operation == "encrypt")
                    {
                        exitCode = AesGcmEncryptor.EncryptFile(filePath, key);
                    }
                    else if (operation == "decrypt")
                    {
                        exitCode = AesGcmEncryptor.DecryptFile(filePath, key);
                    }
                    else
                    {
                        await writer.WriteLineAsync($"ERROR|2|Unknown operation: {operation}");
                        return;
                    }

                    stopwatch.Stop();

                    if (exitCode == 0)
                    {
                        await writer.WriteLineAsync($"OK|0|{stopwatch.ElapsedMilliseconds}");
                        Console.WriteLine($"   {operation} completed in {stopwatch.ElapsedMilliseconds}ms");
                    }
                    else
                    {
                        await writer.WriteLineAsync($"ERROR|{exitCode}|{GetErrorMessage(exitCode)}");
                        Console.WriteLine($"   {operation} failed (code {exitCode})");
                    }
                }
                finally
                {
                    _encryptionSemaphore.Release();
                    Console.WriteLine("   Lock released");
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("   Operation cancelled (shutdown)");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"   Error: {ex.Message}");
        }
        finally
        {
            _activeOperations.TryRemove(operationId, out _);
        }
    }

    private static string GetErrorMessage(int code)
    {
        return code switch
        {
            1 => "File not found",
            2 => "Invalid arguments",
            3 => "I/O error",
            4 => "GCM authentication failure",
            5 => "Invalid key",
            _ => $"Unknown error ({code})"
        };
    }
}
