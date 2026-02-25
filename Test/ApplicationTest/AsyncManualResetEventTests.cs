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
}
