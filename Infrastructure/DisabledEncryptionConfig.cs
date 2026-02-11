using Application.Ports;

namespace Infrastructure;

public class DisabledEncryptionConfig : IEncryptionConfig
{
    public IReadOnlyList<string> GetEncryptedExtensions() => Array.Empty<string>();
    public void SetEncryptedExtensions(IReadOnlyList<string> extensions) { }

    public string GetEncryptionKey() => string.Empty;
    public void SetEncryptionKey(string key) { }
}
