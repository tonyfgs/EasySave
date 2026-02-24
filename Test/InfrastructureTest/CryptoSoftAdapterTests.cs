using Application.Ports;
using Infrastructure;
using Moq;
using System.Net;
using System.Net.Sockets;

namespace InfrastructureTest;

public class CryptoSoftAdapterTests : IDisposable
{
    private readonly string _fakeServerPath;
    private readonly Mock<IEncryptionConfig> _configMock;
    private CryptoSoftAdapter? _adapter;

    public CryptoSoftAdapterTests()
    {
        EnsureDotnetRootIsSet();
        _fakeServerPath = ResolveFakeServerPath();
        _configMock = new Mock<IEncryptionConfig>();
        _configMock.Setup(c => c.GetEncryptionKey()).Returns("dGVzdGtleS0xMjM0NTY3ODkwMTIzNDU2Nzg5MDEyMzQ=");
    }

    public void Dispose()
    {
        _adapter?.Dispose();
    }

    private CryptoSoftAdapter CreateAdapter(int port, string extraArgs = "")
    {
        return new CryptoSoftAdapter(
            _configMock.Object,
            cryptoSoftPath: _fakeServerPath,
            timeoutMs: 10000,
            port: port,
            serverArguments: $"server --port={port} {extraArgs}".Trim());
    }

    [Fact]
    public void EnsureServer_CapturesStderrLines()
    {
        var port = GetAvailablePort();
        _adapter = CreateAdapter(port, "--stderr-rate=10 --stderr-count=20");

        _adapter.EncryptFile("/dev/null");

        // Wait for stderr lines to be captured
        Thread.Sleep(3000);

        var lines = _adapter.GetServerStderrLines();
        Assert.NotEmpty(lines);
    }

    [Fact]
    public void StderrBuffer_ClearedOnNewStartup()
    {
        var port = GetAvailablePort();
        _adapter = CreateAdapter(port, "--stderr-rate=10 --stderr-count=5");

        // First startup
        _adapter.EncryptFile("/dev/null");
        Thread.Sleep(2000);
        var firstLines = _adapter.GetServerStderrLines();
        Assert.NotEmpty(firstLines);

        // Stop and create new adapter (simulates restart)
        _adapter.Dispose();

        var port2 = GetAvailablePort();
        _adapter = CreateAdapter(port2, "--stderr-rate=10 --stderr-count=3");

        _adapter.EncryptFile("/dev/null");
        Thread.Sleep(2000);

        var secondLines = _adapter.GetServerStderrLines();
        // Second run lines should NOT contain first run's numbered stderr lines
        // The startup line "FakeCryptoServer stderr: started on port N" may repeat
        // but with different port numbers
        Assert.NotEmpty(secondLines);
        Assert.True(secondLines.Count <= firstLines.Count + 5,
            "Second run should not accumulate lines from first run");
    }

    [Fact]
    public void StderrBuffer_MaxCapacity200()
    {
        var port = GetAvailablePort();
        _adapter = CreateAdapter(port, "--stderr-rate=100 --stderr-count=300");

        _adapter.EncryptFile("/dev/null");

        // Wait for all 300 lines to be emitted (300 lines at 100/sec = 3 seconds + buffer)
        Thread.Sleep(5000);

        var lines = _adapter.GetServerStderrLines();
        Assert.True(lines.Count <= 200,
            $"Expected at most 200 lines but got {lines.Count}");
    }

    [Fact]
    public void StderrBuffer_LinesAreChronological()
    {
        var port = GetAvailablePort();
        _adapter = CreateAdapter(port, "--stderr-rate=10 --stderr-count=10");

        _adapter.EncryptFile("/dev/null");
        Thread.Sleep(2000);

        var lines = _adapter.GetServerStderrLines();
        // Find numbered lines and verify order
        var numberedLines = lines
            .Where(l => l.Contains("[STDERR] line "))
            .Select(l =>
            {
                var numStr = l.Split("line ").Last();
                return int.TryParse(numStr, out var n) ? n : -1;
            })
            .Where(n => n > 0)
            .ToList();

        for (int i = 1; i < numberedLines.Count; i++)
        {
            Assert.True(numberedLines[i] > numberedLines[i - 1],
                $"Lines not chronological: {numberedLines[i - 1]} followed by {numberedLines[i]}");
        }
    }

    [Fact]
    public void HandlerCleanup_NoDoubleSubscription()
    {
        var observedCounts = new List<int>();

        for (int cycle = 0; cycle < 5; cycle++)
        {
            _adapter?.Dispose();
            var port = GetAvailablePort();
            _adapter = CreateAdapter(port, "--stderr-rate=5 --stderr-count=5");

            _adapter.EncryptFile("/dev/null");
            Thread.Sleep(2000);

            var lines = _adapter.GetServerStderrLines();
            var stderrLogLines = lines.Where(l => l.Contains("[STDERR] line ")).ToList();
            observedCounts.Add(stderrLogLines.Count);
        }

        // Each cycle should observe roughly the same count (no doubling)
        foreach (var count in observedCounts)
        {
            Assert.True(count <= 10,
                $"Observed {count} [STDERR] lines in a cycle, expected <= 10 (5 emitted + startup line). " +
                "This may indicate duplicate handler subscriptions.");
        }
    }

    [Fact]
    public void Dispose_KillsProcess()
    {
        var port = GetAvailablePort();
        _adapter = CreateAdapter(port);

        _adapter.EncryptFile("/dev/null");
        Thread.Sleep(500);

        // Verify server is running
        Assert.True(IsPortInUse(port), "Server should be running before dispose");

        _adapter.Dispose();
        _adapter = null;
        Thread.Sleep(1000);

        // Verify server is stopped
        Assert.False(IsPortInUse(port), "Server should be stopped after dispose");
    }

    [Fact]
    public void Dispose_UnsubscribesHandlers()
    {
        var port = GetAvailablePort();
        _adapter = CreateAdapter(port, "--stderr-rate=5 --stderr-count=50");

        _adapter.EncryptFile("/dev/null");
        Thread.Sleep(1000);

        _adapter.Dispose();

        // After dispose, GetServerStderrLines should return empty (buffer cleared)
        var lines = _adapter.GetServerStderrLines();
        Assert.Empty(lines);

        _adapter = null;
    }

    [Fact]
    public void Dispose_Idempotent()
    {
        var port = GetAvailablePort();
        _adapter = CreateAdapter(port);

        _adapter.EncryptFile("/dev/null");
        Thread.Sleep(500);

        // Double dispose should not throw
        _adapter.Dispose();
        var exception = Record.Exception(() => _adapter.Dispose());

        Assert.Null(exception);
        _adapter = null;
    }

    [Fact]
    public void MemoryStability_100RestartCycles()
    {
        // Warm-up: 5 cycles discarded
        for (int i = 0; i < 5; i++)
        {
            var port = GetAvailablePort();
            using var warmupAdapter = CreateAdapter(port, "--stderr-rate=1 --stderr-count=1");
            warmupAdapter.EncryptFile("/dev/null");
            Thread.Sleep(300);
            warmupAdapter.StopServer();
            Thread.Sleep(200);
        }

        // Double-GC for baseline
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
        var baselineMemory = GC.GetTotalMemory(false);

        // 100 measured cycles
        for (int i = 0; i < 100; i++)
        {
            var port = GetAvailablePort();
            using var cycleAdapter = CreateAdapter(port, "--stderr-rate=1 --stderr-count=2");
            cycleAdapter.EncryptFile("/dev/null");
            Thread.Sleep(200);
            cycleAdapter.StopServer();
            Thread.Sleep(100);
        }

        // Double-GC for final measurement
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
        var finalMemory = GC.GetTotalMemory(false);

        var deltaBytes = finalMemory - baselineMemory;
        Assert.True(deltaBytes <= 5 * 1024 * 1024,
            $"Memory grew by {deltaBytes / 1024.0 / 1024.0:F2}MB over 100 restart cycles (limit: 5MB)");
    }

    [Fact]
    public void EncryptFile_ReturnsSuccess_WithRedirection()
    {
        var port = GetAvailablePort();
        _adapter = CreateAdapter(port, "--stderr-rate=1 --stderr-count=5");

        var result = _adapter.EncryptFile("/dev/null");

        Assert.True(result.Success, $"Expected success but got: {result.ErrorMessage}");
        Assert.Equal(CryptoErrorCode.None, result.ErrorCode);
    }

    private static void EnsureDotnetRootIsSet()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ROOT")))
            return;

        // Derive DOTNET_ROOT from the core library location:
        // e.g. /usr/local/share/dotnet/shared/Microsoft.NETCore.App/8.0.x/System.Private.CoreLib.dll
        // Go up 4 levels: dll -> 8.0.x -> Microsoft.NETCore.App -> shared -> dotnet root
        var coreLibDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var dotnetRoot = Path.GetFullPath(Path.Combine(coreLibDir, "..", "..", ".."));
        Environment.SetEnvironmentVariable("DOTNET_ROOT", dotnetRoot);
    }

    private static string ResolveFakeServerPath()
    {
        var testDir = AppDomain.CurrentDomain.BaseDirectory;
        // Navigate: bin/Debug/net8.0 -> InfrastructureTest -> Test -> solution root
        var solutionDir = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", ".."));
        var path = Path.Combine(solutionDir, "Test", "FakeCryptoServer", "bin", "Debug", "net8.0", "FakeCryptoServer");
        if (OperatingSystem.IsWindows())
            path += ".exe";
        return path;
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static bool IsPortInUse(int port)
    {
        try
        {
            using var client = new TcpClient();
            var task = client.ConnectAsync(IPAddress.Loopback, port);
            return task.Wait(500) && client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
