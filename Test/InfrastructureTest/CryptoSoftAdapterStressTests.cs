using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Application.Ports;
using Infrastructure;
using Moq;

namespace InfrastructureTest;

/// <summary>
/// Generates a deterministic corpus of 100 files with cryptographically random bytes:
///   50 x 1 KB, 30 x 10 KB, 15 x 100 KB, 5 x 500 KB
/// Created once per test class run and deleted in teardown.
/// </summary>
public class CorpusFixture : IDisposable
{
    public string CorpusDirectory { get; }
    public string[] FilePaths { get; }

    public CorpusFixture()
    {
        CorpusDirectory = Path.Combine(Path.GetTempPath(), $"EasySave_StressCorpus_{Guid.NewGuid():N}");
        Directory.CreateDirectory(CorpusDirectory);

        var fileSizes = new List<(string prefix, int sizeBytes, int count)>
        {
            ("1kb", 1024, 50),
            ("10kb", 10240, 30),
            ("100kb", 102400, 15),
            ("500kb", 512000, 5)
        };

        var paths = new List<string>();
        foreach (var (prefix, sizeBytes, count) in fileSizes)
        {
            for (int i = 0; i < count; i++)
            {
                var filePath = Path.Combine(CorpusDirectory, $"{prefix}_{i:D3}.bin");
                var randomBytes = RandomNumberGenerator.GetBytes(sizeBytes);
                File.WriteAllBytes(filePath, randomBytes);
                paths.Add(filePath);
            }
        }

        FilePaths = paths.ToArray();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(CorpusDirectory))
                Directory.Delete(CorpusDirectory, recursive: true);
        }
        catch
        {
            // Best-effort cleanup
        }
    }
}

public class CryptoSoftAdapterStressTests : IClassFixture<CorpusFixture>, IDisposable
{
    private static readonly TimeSpan RunDuration = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(5);
    private const int ConcurrentClients = 50;
    private const int MaxTimeoutMs = 3000;

    private readonly CorpusFixture _corpus;
    private readonly string _fakeServerPath;
    private readonly Mock<IEncryptionConfig> _configMock;
    private CryptoSoftAdapter? _adapter;

    public CryptoSoftAdapterStressTests(CorpusFixture corpus)
    {
        _corpus = corpus;
        EnsureDotnetRootIsSet();
        _fakeServerPath = ResolveFakeServerPath();
        _configMock = new Mock<IEncryptionConfig>();
        _configMock.Setup(c => c.GetEncryptionKey()).Returns("dGVzdGtleS0xMjM0NTY3ODkwMTIzNDU2Nzg5MDEyMzQ=");
    }

    public void Dispose()
    {
        _adapter?.Dispose();
    }

    [Fact]
    [Trait("Category", "Stress")]
    public async Task StressTest_LogInjection_DoesNotDegradeLatency()
    {
        // --- BASELINE RUN: 0 lps log injection, 60s duration ---
        var baselinePort = GetAvailablePort();
        _adapter = CreateAdapter(baselinePort, stderrRate: 0, stdoutRate: 0);

        // Warm up: make one request to ensure server is started
        var warmupResult = _adapter.EncryptFile("/dev/null");
        Assert.True(warmupResult.Success, $"Baseline warmup failed: {warmupResult.ErrorMessage}");

        var (baselineLatencies, baselineTotal, baselineFailed) = await RunConcurrentRequests(_adapter, RunDuration);

        _adapter.Dispose();
        _adapter = null;

        Assert.NotEmpty(baselineLatencies);

        // --- COOLDOWN: 5 seconds ---
        await Task.Delay(Cooldown);

        // --- STRESS RUN: 250 lps stdout + 250 lps stderr (500 total), 60s duration ---
        var stressPort = GetAvailablePort();
        _adapter = CreateAdapter(stressPort, stderrRate: 250, stdoutRate: 250);

        // Warm up: make one request to ensure server is started
        var stressWarmupResult = _adapter.EncryptFile("/dev/null");
        Assert.True(stressWarmupResult.Success, $"Stress warmup failed: {stressWarmupResult.ErrorMessage}");

        var (stressLatencies, stressTotal, stressFailed) = await RunConcurrentRequests(_adapter, RunDuration);

        _adapter.Dispose();
        _adapter = null;

        Assert.NotEmpty(stressLatencies);

        // --- COMPUTE PERCENTILES ---
        var baselineSorted = baselineLatencies.OrderBy(x => x).ToArray();
        var stressSorted = stressLatencies.OrderBy(x => x).ToArray();

        var baselineP95 = Percentile(baselineSorted, 0.95);
        var baselineP99 = Percentile(baselineSorted, 0.99);
        var baselineMax = baselineSorted[^1];
        var stressP95 = Percentile(stressSorted, 0.95);
        var stressP99 = Percentile(stressSorted, 0.99);
        var stressMax = stressSorted[^1];

        // --- ASSERTIONS ---

        // Baseline max must also not exceed timeout (task 7)
        Assert.True(baselineMax <= MaxTimeoutMs,
            $"Baseline max latency exceeded timeout: {baselineMax:F2}ms (limit: {MaxTimeoutMs}ms). " +
            $"Baseline: total={baselineTotal}, failed={baselineFailed}, success={baselineSorted.Length}");

        var deltaP95 = stressP95 - baselineP95;
        var deltaP99 = stressP99 - baselineP99;

        Assert.True(deltaP95 <= 50.0,
            $"p95 delta too high: stress p95={stressP95:F2}ms - baseline p95={baselineP95:F2}ms = {deltaP95:F2}ms (limit: 50ms). " +
            $"Baseline: total={baselineTotal}, failed={baselineFailed}, success={baselineSorted.Length}. " +
            $"Stress: total={stressTotal}, failed={stressFailed}, success={stressSorted.Length}");

        Assert.True(deltaP99 <= 150.0,
            $"p99 delta too high: stress p99={stressP99:F2}ms - baseline p99={baselineP99:F2}ms = {deltaP99:F2}ms (limit: 150ms). " +
            $"Baseline: total={baselineTotal}, failed={baselineFailed}, success={baselineSorted.Length}. " +
            $"Stress: total={stressTotal}, failed={stressFailed}, success={stressSorted.Length}");

        Assert.True(stressMax <= MaxTimeoutMs,
            $"Max latency under stress exceeded timeout: {stressMax:F2}ms (limit: {MaxTimeoutMs}ms). " +
            $"Stress: total={stressTotal}, failed={stressFailed}, success={stressSorted.Length}");

        // Failure rate must be below 1% for both runs
        var baselineFailureRate = baselineTotal > 0 ? (double)baselineFailed / baselineTotal : 0;
        var stressFailureRate = stressTotal > 0 ? (double)stressFailed / stressTotal : 0;

        Assert.True(baselineFailureRate < 0.01,
            $"Baseline failure rate {baselineFailureRate:P2} exceeds 1% ({baselineFailed}/{baselineTotal})");

        Assert.True(stressFailureRate < 0.01,
            $"Stress failure rate {stressFailureRate:P2} exceeds 1% ({stressFailed}/{stressTotal})");

        // No deadlock: if we reach this point, no deadlock or hang occurred
        // (the test would have timed out otherwise)
    }

    /// <summary>
    /// Runs <see cref="ConcurrentClients"/> concurrent tasks, each performing encrypt requests
    /// in a tight loop for the specified duration. Files are chosen via deterministic round-robin
    /// from the corpus. Each request measures round-trip TCP latency.
    /// Returns (latencies, totalRequests, failedRequests).
    /// </summary>
    private async Task<(ConcurrentBag<double> latencies, int totalRequests, int failedRequests)>
        RunConcurrentRequests(CryptoSoftAdapter adapter, TimeSpan duration)
    {
        var latencies = new ConcurrentBag<double>();
        var totalRequests = 0;
        var failedRequests = 0;
        var deadline = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, ConcurrentClients).Select(clientIndex =>
        {
            return Task.Run(() =>
            {
                var requestIndex = 0;
                var corpusLength = _corpus.FilePaths.Length;

                while (deadline.Elapsed < duration)
                {
                    var fileIndex = (clientIndex + requestIndex * ConcurrentClients) % corpusLength;
                    var sourceFile = _corpus.FilePaths[fileIndex];

                    // Spec protocol: copy shared source -> encrypt temp copy -> delete temp copy
                    var tempFile = Path.Combine(Path.GetTempPath(),
                        $"easysave_stress_{clientIndex}_{requestIndex}_{Path.GetFileName(sourceFile)}");
                    File.Copy(sourceFile, tempFile, overwrite: true);

                    var sw = Stopwatch.StartNew();
                    var result = adapter.EncryptFile(tempFile);
                    sw.Stop();

                    try { File.Delete(tempFile); } catch { /* best-effort cleanup */ }

                    Interlocked.Increment(ref totalRequests);

                    if (result.Success)
                    {
                        latencies.Add(sw.Elapsed.TotalMilliseconds);
                    }
                    else
                    {
                        Interlocked.Increment(ref failedRequests);
                    }

                    requestIndex++;
                }
            });
        }).ToArray();

        await Task.WhenAll(tasks);
        return (latencies, totalRequests, failedRequests);
    }

    /// <summary>
    /// Computes the percentile value from a pre-sorted array using nearest-rank method.
    /// </summary>
    private static double Percentile(double[] sorted, double percentile)
    {
        if (sorted.Length == 0)
            return 0;

        var index = (int)Math.Ceiling(percentile * sorted.Length) - 1;
        index = Math.Max(0, Math.Min(index, sorted.Length - 1));
        return sorted[index];
    }

    private CryptoSoftAdapter CreateAdapter(int port, int stderrRate, int stdoutRate)
    {
        return new CryptoSoftAdapter(
            _configMock.Object,
            cryptoSoftPath: _fakeServerPath,
            timeoutMs: MaxTimeoutMs,
            port: port,
            serverArguments: $"server --port={port} --stderr-rate={stderrRate} --stdout-rate={stdoutRate}");
    }

    private static void EnsureDotnetRootIsSet()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_ROOT")))
            return;

        var coreLibDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var dotnetRoot = Path.GetFullPath(Path.Combine(coreLibDir, "..", "..", ".."));
        Environment.SetEnvironmentVariable("DOTNET_ROOT", dotnetRoot);
    }

    private static string ResolveFakeServerPath()
    {
        var testDir = AppDomain.CurrentDomain.BaseDirectory;
        var solutionDir = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", ".."));
        var configuration = new DirectoryInfo(testDir).Parent!.Name;
        var path = Path.Combine(solutionDir, "Test", "FakeCryptoServer", "bin", configuration, "net8.0", "FakeCryptoServer");
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
}
