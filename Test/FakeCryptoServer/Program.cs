using System.Net;
using System.Net.Sockets;
using System.Text;

// Usage: FakeCryptoServer server [--port=N] [--stderr-rate=N] [--stdout-rate=N] [--stderr-count=N] [--no-listen]
// Mimics CryptoSoft TCP server protocol while emitting configurable log lines.

var port = 19283;
var stderrRate = 0;   // lines per second (0 = no emission)
var stdoutRate = 0;   // lines per second (0 = no emission)
var stderrCount = int.MaxValue; // total stderr lines to emit before stopping
var noListen = false; // when true, skip TCP listener (simulate startup failure)

foreach (var arg in args)
{
    if (arg.StartsWith("--port=")) port = int.Parse(arg["--port=".Length..]);
    if (arg.StartsWith("--stderr-rate=")) stderrRate = int.Parse(arg["--stderr-rate=".Length..]);
    if (arg.StartsWith("--stdout-rate=")) stdoutRate = int.Parse(arg["--stdout-rate=".Length..]);
    if (arg.StartsWith("--stderr-count=")) stderrCount = int.Parse(arg["--stderr-count=".Length..]);
    if (arg == "--no-listen") noListen = true;
}

var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// Log emitter task factory (shared between normal and no-listen modes)
Task StartLogEmitter() => Task.Run(async () =>
{
    var stderrEmitted = 0;
    var stdoutEmitted = 0;

    while (!cts.Token.IsCancellationRequested)
    {
        if (stderrRate > 0 && stderrEmitted < stderrCount)
        {
            for (var i = 0; i < stderrRate && stderrEmitted < stderrCount; i++)
            {
                Console.Error.WriteLine($"[STDERR] line {++stderrEmitted}");
            }
        }

        if (stdoutRate > 0)
        {
            for (var i = 0; i < stdoutRate; i++)
            {
                Console.WriteLine($"[STDOUT] line {++stdoutEmitted}");
            }
        }

        try
        {
            await Task.Delay(1000, cts.Token);
        }
        catch (OperationCanceledException)
        {
            break;
        }
    }
});

if (noListen)
{
    // No-listen mode: emit logs but never open a TCP port (simulates startup failure)
    Console.WriteLine($"FakeCryptoServer started in no-listen mode (port {port} NOT bound)");
    Console.Error.WriteLine($"FakeCryptoServer stderr: started in no-listen mode");

    var logTask = StartLogEmitter();

    try
    {
        await Task.Delay(Timeout.Infinite, cts.Token);
    }
    catch (OperationCanceledException)
    {
        // Graceful shutdown via Ctrl+C
    }

    return 0;
}

TcpListener? listener = null;
try
{
    listener = new TcpListener(IPAddress.Loopback, port);
    listener.Start();

    Console.WriteLine($"FakeCryptoServer started on port {port}");
    Console.Error.WriteLine($"FakeCryptoServer stderr: started on port {port}");

    // Background log emitter
    var logTask = StartLogEmitter();

    // Accept TCP connections (mimic CryptoServer protocol)
    while (!cts.Token.IsCancellationRequested)
    {
        if (listener.Pending())
        {
            var client = listener.AcceptTcpClient();
            _ = Task.Run(async () =>
            {
                try
                {
                    using (client)
                    await using (var stream = client.GetStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    await using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    {
                        stream.ReadTimeout = 30000;
                        stream.WriteTimeout = 30000;

                        var request = await reader.ReadLineAsync();
                        if (string.IsNullOrEmpty(request))
                        {
                            await writer.WriteLineAsync("ERROR|2|Empty request");
                            return;
                        }

                        // Always respond OK with 1ms duration
                        await writer.WriteLineAsync("OK|0|1");
                    }
                }
                catch
                {
                    // Ignore client errors
                }
            });
        }
        else
        {
            Thread.Sleep(50);
        }
    }
}
catch (SocketException ex)
{
    Console.Error.WriteLine($"FakeCryptoServer: port {port} unavailable: {ex.Message}");
    return 3;
}
finally
{
    listener?.Stop();
}

return 0;
