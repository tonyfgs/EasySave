using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Text;

namespace CryptoSoft;

[SupportedOSPlatform("windows")]
public class CryptoClient
{
    private const string PipeName = "CryptoSoftPipe";
    private readonly int _timeoutMs;

    public CryptoClient(int timeoutMs = 300000) // 5 minutes par défaut
    {
        _timeoutMs = timeoutMs;
    }

    public record CryptoResponse(bool Success, int ExitCode, long DurationMs, string? ErrorMessage);

    public CryptoResponse Encrypt(string filePath, string key)
    {
        return SendRequest("encrypt", filePath, key);
    }

    public CryptoResponse Decrypt(string filePath, string key)
    {
        return SendRequest("decrypt", filePath, key);
    }

    private CryptoResponse SendRequest(string operation, string filePath, string key)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut);

            // Connexion au serveur avec timeout
            pipe.Connect(_timeoutMs);
            pipe.ReadMode = PipeTransmissionMode.Message;

            using var writer = new StreamWriter(pipe, Encoding.UTF8) { AutoFlush = true };
            using var reader = new StreamReader(pipe, Encoding.UTF8);

            // Envoyer la requête
            writer.WriteLine($"{operation}|{filePath}|{key}");

            // Lire la réponse
            var response = reader.ReadLine();
            stopwatch.Stop();

            if (string.IsNullOrEmpty(response))
            {
                return new CryptoResponse(false, 3, stopwatch.ElapsedMilliseconds, "Pas de réponse du serveur");
            }

            // Parser la réponse (format: "OK|0|durationMs" ou "ERROR|code|message")
            var parts = response.Split('|');
            if (parts.Length < 3)
            {
                return new CryptoResponse(false, 3, stopwatch.ElapsedMilliseconds, "Réponse invalide du serveur");
            }

            var status = parts[0];
            var code = int.TryParse(parts[1], out var c) ? c : 3;

            if (status == "OK")
            {
                var serverDuration = long.TryParse(parts[2], out var d) ? d : stopwatch.ElapsedMilliseconds;
                return new CryptoResponse(true, 0, serverDuration, null);
            }
            else
            {
                return new CryptoResponse(false, code, stopwatch.ElapsedMilliseconds, parts[2]);
            }
        }
        catch (TimeoutException)
        {
            return new CryptoResponse(false, 6, stopwatch.ElapsedMilliseconds, "Timeout: impossible de se connecter au serveur CryptoSoft");
        }
        catch (IOException ex)
        {
            return new CryptoResponse(false, 3, stopwatch.ElapsedMilliseconds, $"Erreur I/O: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new CryptoResponse(false, 3, stopwatch.ElapsedMilliseconds, $"Erreur: {ex.Message}");
        }
    }

    public static bool IsServerRunning()
    {
        return CryptoServer.IsServerRunning();
    }
}

