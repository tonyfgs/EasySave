using System.Collections.Concurrent;
using Application.Concurrency;

namespace ApplicationTest;

public class AsyncManualResetEventTests
{
    [Fact]
    public void Constructor_WithDefaultState_IsSetTrue()
    {
        var mre = new AsyncManualResetEvent();

        Assert.True(mre.IsSet);
    }

    [Fact]
    public void Constructor_WithFalseState_IsSetFalse()
    {
        var mre = new AsyncManualResetEvent(initialState: false);

        Assert.False(mre.IsSet);
    }

    [Fact]
    public async Task WaitAsync_WhenSignaled_ReturnsCompletedTask()
    {
        var mre = new AsyncManualResetEvent();

        var waitTask = mre.WaitAsync();

        Assert.True(waitTask.IsCompleted);
        await waitTask;
    }

    [Fact]
    public async Task WaitAsync_WhenUnsignaled_DoesNotCompleteWithin200ms()
    {
        var mre = new AsyncManualResetEvent(initialState: false);

        var waitTask = mre.WaitAsync();

        var completedTask = await Task.WhenAny(waitTask, Task.Delay(200));
        Assert.NotEqual(waitTask, completedTask);
    }

    [Fact]
    public async Task Set_WhenWaiterPending_UnblocksWithin100ms()
    {
        var mre = new AsyncManualResetEvent(initialState: false);
        var waitTask = mre.WaitAsync();

        mre.Set();

        var completedTask = await Task.WhenAny(waitTask, Task.Delay(100));
        Assert.Equal(waitTask, completedTask);
        await waitTask;
    }

    [Fact]
    public async Task WaitAsync_FullSignalCycle_SuspendsAfterResetAndCompletesAfterSet()
    {
        var mre = new AsyncManualResetEvent();
        Assert.True(mre.IsSet);

        mre.Reset();
        var waitTask = mre.WaitAsync();

        var race1 = await Task.WhenAny(waitTask, Task.Delay(200));
        Assert.NotEqual(waitTask, race1);

        mre.Set();

        var race2 = await Task.WhenAny(waitTask, Task.Delay(100));
        Assert.Equal(waitTask, race2);
        await waitTask;
    }

    [Fact]
    public void IsSet_AfterSetAndReset_ReflectsCurrentState()
    {
        var mre = new AsyncManualResetEvent();
        Assert.True(mre.IsSet);

        mre.Reset();
        Assert.False(mre.IsSet);

        mre.Set();
        Assert.True(mre.IsSet);
    }

    [Fact]
    public async Task Set_CalledTwice_RemainsSignaled()
    {
        var mre = new AsyncManualResetEvent();

        mre.Set();
        mre.Set();

        Assert.True(mre.IsSet);
        var waitTask = mre.WaitAsync();
        Assert.True(waitTask.IsCompleted);
        await waitTask;
    }

    [Fact]
    public async Task Reset_CalledTwice_RemainsUnsignaled()
    {
        var mre = new AsyncManualResetEvent(initialState: false);

        mre.Reset();
        mre.Reset();

        Assert.False(mre.IsSet);
        var waitTask = mre.WaitAsync();
        var completedTask = await Task.WhenAny(waitTask, Task.Delay(200));
        Assert.NotEqual(waitTask, completedTask);
    }

    [Fact]
    public async Task WaitAsync_WithPreCancelledToken_ThrowsOperationCanceledException()
    {
        var mre = new AsyncManualResetEvent(initialState: false);
        var cancelledToken = new CancellationToken(canceled: true);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => mre.WaitAsync(cancelledToken));
    }

    [Fact]
    public async Task WaitAsync_WithTokenCancelledDuringWait_ThrowsOperationCanceledException()
    {
        var mre = new AsyncManualResetEvent(initialState: false);
        using var cts = new CancellationTokenSource();

        var waitTask = mre.WaitAsync(cts.Token);

        cts.CancelAfter(50);

        var exceptionTask = Assert.ThrowsAsync<OperationCanceledException>(() => waitTask);
        var completedTask = await Task.WhenAny(exceptionTask, Task.Delay(300));
        Assert.Equal(exceptionTask, completedTask);
        await exceptionTask;
    }

    [Fact]
    public async Task StressTest_ConcurrentSetResetAndWait_CompletesWithin5Seconds()
    {
        var mre = new AsyncManualResetEvent();
        const int setterCount = 10;
        const int waiterCount = 10;
        const int iterations = 1000;
        var exceptions = new ConcurrentBag<Exception>();

        var setterTasks = Enumerable.Range(0, setterCount).Select(_ => Task.Run(() =>
        {
            try
            {
                for (int i = 0; i < iterations; i++)
                {
                    mre.Set();
                    mre.Reset();
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        var waiterTasks = Enumerable.Range(0, waiterCount).Select(_ => Task.Run(async () =>
        {
            try
            {
                for (int i = 0; i < iterations; i++)
                {
                    await mre.WaitAsync();
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        // After all setters finish, signal once so waiters drain
        // (event stays signaled since no more Resets, all WaitAsync hit fast path)
        _ = Task.WhenAll(setterTasks).ContinueWith(_ => mre.Set());

        var allTasks = Task.WhenAll(setterTasks.Concat(waiterTasks));
        var completedTask = await Task.WhenAny(allTasks, Task.Delay(5000));

        Assert.Equal(allTasks, completedTask);
        Assert.Empty(exceptions);
    }
}
