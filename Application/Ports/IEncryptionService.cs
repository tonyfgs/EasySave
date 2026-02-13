namespace Application.Ports;

public interface IEncryptionService
{
    CryptoResult EncryptFile(string filePath);
    CryptoResult DecryptFile(string filePath);
}

public sealed class CryptoResult
{
    public bool Success { get; init; }
    public long DurationMs { get; init; }
    public CryptoErrorCode ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}

public enum CryptoErrorCode
{
    None,
    FileNotFound,
    InvalidArguments,
    IoError,
    AuthTagInvalid,
    InvalidKey,
    Timeout,
    Unknown
}
