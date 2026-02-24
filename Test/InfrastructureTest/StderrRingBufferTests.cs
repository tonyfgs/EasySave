using Infrastructure;

namespace InfrastructureTest;

public class StderrRingBufferTests
{
    [Fact]
    public void Append_SingleLine_ReturnsIt()
    {
        var buffer = new StderrRingBuffer();

        buffer.Append("line 1");

        var lines = buffer.GetLines();
        Assert.Single(lines);
        Assert.Equal("line 1", lines[0]);
    }

    [Fact]
    public void Append_AtCapacity_OldestDropped()
    {
        var buffer = new StderrRingBuffer(capacity: 3);

        buffer.Append("a");
        buffer.Append("b");
        buffer.Append("c");
        buffer.Append("d");

        var lines = buffer.GetLines();
        Assert.Equal(3, lines.Count);
        Assert.Equal("b", lines[0]);
        Assert.Equal("c", lines[1]);
        Assert.Equal("d", lines[2]);
    }

    [Fact]
    public void Append_OverCapacity_MaintainsSize()
    {
        var buffer = new StderrRingBuffer(capacity: 200);

        for (int i = 0; i < 300; i++)
            buffer.Append($"line {i}");

        Assert.Equal(200, buffer.Count);

        var lines = buffer.GetLines();
        Assert.Equal("line 100", lines[0]);
        Assert.Equal("line 299", lines[199]);
    }

    [Fact]
    public void Clear_EmptiesBuffer()
    {
        var buffer = new StderrRingBuffer();
        buffer.Append("a");
        buffer.Append("b");

        buffer.Clear();

        Assert.Equal(0, buffer.Count);
        Assert.Empty(buffer.GetLines());
    }

    [Fact]
    public void GetLines_ReturnsChronologicalOrder()
    {
        var buffer = new StderrRingBuffer();

        buffer.Append("first");
        buffer.Append("second");
        buffer.Append("third");

        var lines = buffer.GetLines();
        Assert.Equal("first", lines[0]);
        Assert.Equal("second", lines[1]);
        Assert.Equal("third", lines[2]);
    }

    [Fact]
    public void GetLines_ReturnsDefensiveCopy()
    {
        var buffer = new StderrRingBuffer();
        buffer.Append("original");

        var lines1 = buffer.GetLines();
        buffer.Append("added");

        var lines2 = buffer.GetLines();
        Assert.Single(lines1);
        Assert.Equal(2, lines2.Count);
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentAppends()
    {
        var buffer = new StderrRingBuffer(capacity: 200);
        var tasks = new Task[10];

        for (int t = 0; t < 10; t++)
        {
            var threadId = t;
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                    buffer.Append($"thread{threadId}-line{i}");
            });
        }

        await Task.WhenAll(tasks);

        Assert.True(buffer.Count <= 200);
        Assert.True(buffer.Count > 0);
        var lines = buffer.GetLines();
        Assert.Equal(buffer.Count, lines.Count);
    }

    [Fact]
    public void Count_ReflectsActualSize()
    {
        var buffer = new StderrRingBuffer(capacity: 5);

        Assert.Equal(0, buffer.Count);

        buffer.Append("a");
        Assert.Equal(1, buffer.Count);

        buffer.Append("b");
        buffer.Append("c");
        Assert.Equal(3, buffer.Count);

        buffer.Append("d");
        buffer.Append("e");
        buffer.Append("f");
        Assert.Equal(5, buffer.Count);
    }

    [Fact]
    public void Constructor_CustomCapacity()
    {
        var buffer = new StderrRingBuffer(capacity: 5);

        for (int i = 0; i < 10; i++)
            buffer.Append($"line {i}");

        Assert.Equal(5, buffer.Count);
        var lines = buffer.GetLines();
        Assert.Equal("line 5", lines[0]);
        Assert.Equal("line 9", lines[4]);
    }

    [Fact]
    public void EmptyBuffer_ReturnsEmptyList()
    {
        var buffer = new StderrRingBuffer();

        var lines = buffer.GetLines();

        Assert.NotNull(lines);
        Assert.Empty(lines);
    }
}
