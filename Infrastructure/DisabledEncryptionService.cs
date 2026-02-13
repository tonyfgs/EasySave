using Application.Ports;

namespace Infrastructure;

public class DisabledEncryptionService : IEncryptionService
{
    public CryptoResult EncryptFile(string filePath) =>
        new() { Success = true, DurationMs = 0, ErrorCode = CryptoErrorCode.None };

    public CryptoResult DecryptFile(string filePath) =>
        new() { Success = true, DurationMs = 0, ErrorCode = CryptoErrorCode.None };
}
