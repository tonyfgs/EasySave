using Application.Concurrency;

namespace ApplicationTest;

public class PriorityFileGateTests
{
    [Fact]
    public async Task WaitAsync_NoPriorityRegistered_ReturnsImmediately()
    {
        var gate = new PriorityFileGate();

        var task = gate.WaitForPriorityCompletionAsync(CancellationToken.None);

        Assert.True(task.IsCompleted);
        await task; // should not throw
    }

    [Fact]
    public async Task WaitAsync_WithPriority_BlocksUntilAllCompleted()
    {
        var gate = new PriorityFileGate();
        gate.RegisterPriorityFiles(2);

        var waitTask = gate.WaitForPriorityCompletionAsync(CancellationToken.None);
        Assert.False(waitTask.IsCompleted);

        gate.PriorityFileCompleted();
        Assert.False(waitTask.IsCompleted);

        gate.PriorityFileCompleted();
        await waitTask; // should complete now
        Assert.True(waitTask.IsCompleted);
    }

    [Fact]
    public async Task WaitAsync_MultiJobRegistration_BlocksUntilAllDone()
    {
        var gate = new PriorityFileGate();
        gate.RegisterPriorityFiles(1); // job A
        gate.RegisterPriorityFiles(2); // job B

        var waitTask = gate.WaitForPriorityCompletionAsync(CancellationToken.None);
        Assert.False(waitTask.IsCompleted);

        gate.PriorityFileCompleted(); // 2 remaining
        gate.PriorityFileCompleted(); // 1 remaining
        Assert.False(waitTask.IsCompleted);

        gate.PriorityFileCompleted(); // 0 remaining
        await waitTask;
        Assert.True(waitTask.IsCompleted);
    }

    [Fact]
    public async Task WaitAsync_CancellationToken_Honored()
    {
        var gate = new PriorityFileGate();
        gate.RegisterPriorityFiles(1);

        using var cts = new CancellationTokenSource();
        var waitTask = gate.WaitForPriorityCompletionAsync(cts.Token);
        Assert.False(waitTask.IsCompleted);

        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => waitTask);
    }

    [Fact]
    public void PriorityFileCompleted_WhenAlreadyZero_DoesNotGoNegative()
    {
        var gate = new PriorityFileGate();
        gate.RegisterPriorityFiles(1);
        gate.PriorityFileCompleted();

        // Extra call should not go negative
        gate.PriorityFileCompleted();
        gate.PriorityFileCompleted();

        Assert.Equal(0, gate.RemainingCount);
    }

    [Fact]
    public async Task ReleasePriorityFiles_OpensGate()
    {
        var gate = new PriorityFileGate();
        gate.RegisterPriorityFiles(5);

        var waitTask = gate.WaitForPriorityCompletionAsync(CancellationToken.None);
        Assert.False(waitTask.IsCompleted);

        gate.ReleasePriorityFiles(5);
        await waitTask;
        Assert.Equal(0, gate.RemainingCount);
    }

    [Fact]
    public async Task ReleasePriorityFiles_PartialRelease_DoesNotOpen()
    {
        var gate = new PriorityFileGate();
        gate.RegisterPriorityFiles(5);

        gate.ReleasePriorityFiles(3);
        Assert.Equal(2, gate.RemainingCount);

        var waitTask = gate.WaitForPriorityCompletionAsync(CancellationToken.None);
        Assert.False(waitTask.IsCompleted);

        gate.ReleasePriorityFiles(2);
        await waitTask;
        Assert.Equal(0, gate.RemainingCount);
    }

    [Fact]
    public async Task ConcurrentAccess_ThreadSafe()
    {
        var gate = new PriorityFileGate();
        const int fileCount = 100;
        gate.RegisterPriorityFiles(fileCount);

        var waitTask = gate.WaitForPriorityCompletionAsync(CancellationToken.None);

        var tasks = Enumerable.Range(0, fileCount)
            .Select(_ => Task.Run(() => gate.PriorityFileCompleted()));
        await Task.WhenAll(tasks);

        await waitTask;
        Assert.Equal(0, gate.RemainingCount);
    }

    [Fact]
    public void RegisterPriorityFiles_ZeroOrNegative_NoOp()
    {
        var gate = new PriorityFileGate();

        gate.RegisterPriorityFiles(0);
        gate.RegisterPriorityFiles(-5);

        Assert.Equal(0, gate.RemainingCount);
    }
}
