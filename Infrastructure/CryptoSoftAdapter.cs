using System.Diagnostics;
using Application.Ports;

namespace Infrastructure;

public class CryptoSoftAdapter : IEncryptionService
{
    private readonly IEncryptionConfig _config;
    private readonly string _cryptoSoftPath;
    private readonly int _timeoutMs;
    private readonly int _maxRetries;
    private readonly int _initialRetryDelayMs;

    public CryptoSoftAdapter(
        IEncryptionConfig config, 
        string cryptoSoftPath, 
        int timeoutMs = 300000,
        int maxRetries = 5,
        int initialRetryDelayMs = 100)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _cryptoSoftPath = cryptoSoftPath ?? throw new ArgumentNullException(nameof(cryptoSoftPath));
        _timeoutMs = timeoutMs;
        _maxRetries = maxRetries;
        _initialRetryDelayMs = initialRetryDelayMs;
    }

    public CryptoResult EncryptFile(string filePath)
    {
        return ExecuteWithRetry("encrypt", filePath);
    }

    public CryptoResult DecryptFile(string filePath)
    {
        return ExecuteWithRetry("decrypt", filePath);
    }

    private CryptoResult ExecuteWithRetry(string subcommand, string filePath)
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

        var totalStopwatch = Stopwatch.StartNew();
        int retryCount = 0;
        int currentDelayMs = _initialRetryDelayMs;

        while (true)
        {
            var result = ExecuteCryptoSoft(subcommand, filePath, key);

            // Si succès ou erreur autre que AlreadyRunning, retourner immédiatement
            if (result.Success || result.ErrorCode != CryptoErrorCode.AlreadyRunning)
            {
                return result;
            }

            // Vérifier si on a atteint le nombre max de retries
            if (retryCount >= _maxRetries)
            {
                return new CryptoResult
                {
                    Success = false,
                    DurationMs = totalStopwatch.ElapsedMilliseconds,
                    ErrorCode = CryptoErrorCode.AlreadyRunning,
                    ErrorMessage = $"CryptoSoft toujours occupé après {_maxRetries} tentatives"
                };
            }

            // Vérifier si on dépasse le timeout total
            if (totalStopwatch.ElapsedMilliseconds + currentDelayMs > _timeoutMs)
            {
                return new CryptoResult
                {
                    Success = false,
                    DurationMs = totalStopwatch.ElapsedMilliseconds,
                    ErrorCode = CryptoErrorCode.Timeout,
                    ErrorMessage = $"Timeout atteint en attendant que CryptoSoft soit disponible"
                };
            }

            // Exponential backoff
            Thread.Sleep(currentDelayMs);
            currentDelayMs = Math.Min(currentDelayMs * 2, 5000); // Cap à 5 secondes
            retryCount++;
        }
    }

    private CryptoResult ExecuteCryptoSoft(string subcommand, string filePath, string key)
    {
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
            6 => CryptoErrorCode.AlreadyRunning,
            _ => CryptoErrorCode.Unknown
        };
    }
}

