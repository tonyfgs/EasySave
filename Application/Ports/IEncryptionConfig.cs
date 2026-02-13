namespace Application.Ports;

public interface IEncryptionConfig
{
    IReadOnlyList<string> GetEncryptedExtensions();
    void SetEncryptedExtensions(IReadOnlyList<string> extensions);

    string GetEncryptionKey();
    void SetEncryptionKey(string key);
}
