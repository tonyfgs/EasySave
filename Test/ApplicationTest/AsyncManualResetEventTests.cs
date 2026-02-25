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
}
