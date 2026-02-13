using System.Diagnostics;
using Application.Ports;

namespace Infrastructure;

public class CryptoSoftAdapter : IEncryptionService
{
    private readonly IEncryptionConfig _config;
    private readonly string _cryptoSoftPath;
    private readonly int _timeoutMs;

    public CryptoSoftAdapter(IEncryptionConfig config, string cryptoSoftPath, int timeoutMs = 300000)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _cryptoSoftPath = cryptoSoftPath ?? throw new ArgumentNullException(nameof(cryptoSoftPath));
        _timeoutMs = timeoutMs;
    }

    public CryptoResult EncryptFile(string filePath)
    {
        return ExecuteCryptoSoft("encrypt", filePath);
    }

    public CryptoResult DecryptFile(string filePath)
    {
        return ExecuteCryptoSoft("decrypt", filePath);
    }

    private CryptoResult ExecuteCryptoSoft(string subcommand, string filePath)
    {
        var key = _config.GetEncryptionKey();

        // Si clé vide -> pas de chiffrement, succès immédiat
        if (string.IsNullOrWhiteSpace(key))
        {
            return new CryptoResult
            {
                Success = true,
                DurationMs = 0,
                ErrorCode = CryptoErrorCode.None,
                ErrorMessage = null
            };
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = _cryptoSoftPath,
                Arguments = $"{subcommand} \"{filePath}\" \"{key}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            process.Start();

            var exited = process.WaitForExit(_timeoutMs);
            stopwatch.Stop();

            if (!exited)
            {
                try { process.Kill(); } catch { /* Ignorer les erreurs de kill */ }
                return new CryptoResult
                {
                    Success = false,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    ErrorCode = CryptoErrorCode.Timeout,
                    ErrorMessage = $"CryptoSoft timeout après {_timeoutMs}ms"
                };
            }

            var exitCode = process.ExitCode;
            var errorCode = MapExitCode(exitCode);

            if (exitCode == 0)
            {
                return new CryptoResult
                {
                    Success = true,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    ErrorCode = CryptoErrorCode.None,
                    ErrorMessage = null
                };
            }

            var errorOutput = process.StandardError.ReadToEnd();
            return new CryptoResult
            {
                Success = false,
                DurationMs = stopwatch.ElapsedMilliseconds,
                ErrorCode = errorCode,
                ErrorMessage = string.IsNullOrWhiteSpace(errorOutput)
                    ? $"CryptoSoft {subcommand} échoué avec code {exitCode}"
                    : errorOutput.Trim()
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new CryptoResult
            {
                Success = false,
                DurationMs = stopwatch.ElapsedMilliseconds,
                ErrorCode = CryptoErrorCode.Unknown,
                ErrorMessage = $"Erreur lors de l'exécution de CryptoSoft: {ex.Message}"
            };
        }
    }

    private static CryptoErrorCode MapExitCode(int exitCode)
    {
        return exitCode switch
        {
            0 => CryptoErrorCode.None,
            1 => CryptoErrorCode.FileNotFound,
            2 => CryptoErrorCode.InvalidArguments,
            3 => CryptoErrorCode.IoError,
            4 => CryptoErrorCode.AuthTagInvalid,
            5 => CryptoErrorCode.InvalidKey,
            _ => CryptoErrorCode.Unknown
        };
    }
}

