using Infrastructure;

namespace InfrastructureTest;

public class DisabledEncryptionConfigTests
{
    private readonly DisabledEncryptionConfig _config = new();

    [Fact]
    public void GetEncryptedExtensions_ShouldReturnEmpty()
    {
        Assert.Empty(_config.GetEncryptedExtensions());
    }

    [Fact]
    public void GetEncryptionKey_ShouldReturnEmpty()
    {
        Assert.Equal(string.Empty, _config.GetEncryptionKey());
    }
}
