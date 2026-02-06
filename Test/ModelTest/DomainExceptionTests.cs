using Model;

namespace ModelTest;

public class DomainExceptionTests
{
    [Fact]
    public void DomainException_ShouldContainMessage()
    {
        var ex = new DomainException("something went wrong");
        Assert.Equal("something went wrong", ex.Message);
    }

    [Fact]
    public void InvalidBackupJobException_ShouldContainReason()
    {
        var ex = new InvalidBackupJobException("name is empty");
        Assert.Equal("name is empty", ex.Message);
        Assert.IsAssignableFrom<DomainException>(ex);
    }

    [Fact]
    public void JobLimitExceededException_ShouldContainMax()
    {
        var ex = new JobLimitExceededException(5);
        Assert.Contains("5", ex.Message);
        Assert.IsAssignableFrom<DomainException>(ex);
    }
}
