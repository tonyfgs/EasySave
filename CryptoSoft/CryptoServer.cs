using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CryptoSoft;

/// <summary>
/// Serveur CryptoSoft mono-instance utilisant TCP localhost (cross-platform).
/// Accepte plusieurs connexions simultanées mais traite les encryptions une à une.
/// </summary>
public class CryptoServer
{
    public const int DefaultPort = 19283;
    private const string MutexName = "Global\\CryptoSoftServerMutex";
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, Task> _activeOperations = new();

    // SemaphoreSlim pour garantir une seule encryption à la fois (mono-instance)
    private readonly SemaphoreSlim _encryptionSemaphore = new(1, 1);

    private readonly int _port;

    public CryptoServer(int port = DefaultPort)
    {
        _port = port;
    }

    /// <summary>
    /// Vérifie si le serveur est en cours d'exécution via le port TCP.
    /// </summary>
    public static bool IsServerRunning(int port = DefaultPort)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(IPAddress.Loopback, port);
            if (connectTask.Wait(500))
            {
                return client.Connected;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public int Run()
    {
        // Utiliser un Mutex pour éviter plusieurs serveurs sur la même machine (Windows only)
        Mutex? mutex = null;
        bool createdNew = false;

        if (OperatingSystem.IsWindows())
        {
            mutex = new Mutex(true, MutexName, out createdNew);
            if (!createdNew)
            {
                Console.Error.WriteLine("CryptoSoft Server est déjà en cours d'exécution.");
                return 6;
            }
        }

        TcpListener? listener = null;
        try
        {
            listener = new TcpListener(IPAddress.Loopback, _port);
            listener.Start();

            Console.WriteLine("🔐 CryptoSoft Server démarré");
            Console.WriteLine($"   Port: {_port} (TCP localhost)");
            Console.WriteLine("   Cross-platform: Windows, Linux, macOS");
            Console.WriteLine("   Mono-instance: Les encryptions sont traitées une à une");
            Console.WriteLine("   En attente de connexions...");
            Console.WriteLine("   Appuyez sur Ctrl+C pour arrêter.");
            Console.WriteLine();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                _cts.Cancel();
                Console.WriteLine("\n⏹ Arrêt demandé...");
            };

            // Accepter plusieurs connexions en parallèle
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    // Polling pour pouvoir réagir à l'annulation
                    if (listener.Pending())
                    {
                        var client = listener.AcceptTcpClient();
                        // Stocker la vraie task pour le shutdown propre
                        var operationId = Guid.NewGuid().ToString();
                        var task = Task.Run(() => HandleClientAsync(client, operationId), _cts.Token);
                        _activeOperations[operationId] = task;
                    }
                    else
                    {
                        Thread.Sleep(50); // Polling interval
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException)
                {
                    break;
                }
            }
        }
        catch (SocketException ex)
        {
            Console.Error.WriteLine($"Erreur: Port {_port} déjà utilisé ou indisponible.");
            Console.Error.WriteLine($"Détails: {ex.Message}");
            return 3;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erreur serveur: {ex.Message}");
            return 3;
        }
        finally
        {
            listener?.Stop();

            // Attendre que toutes les opérations en cours se terminent
            var tasks = _activeOperations.Values.ToArray();
            if (tasks.Length > 0)
            {
                Console.WriteLine($"⏳ Attente de {tasks.Length} opération(s) en cours...");
                Task.WaitAll(tasks, TimeSpan.FromSeconds(30));
            }

            _encryptionSemaphore.Dispose();

            if (OperatingSystem.IsWindows() && mutex != null)
            {
                mutex.ReleaseMutex();
                mutex.Dispose();
            }
        }

        Console.WriteLine("✓ CryptoSoft Server arrêté proprement.");
        return 0;
    }

    private async Task HandleClientAsync(TcpClient tcpClient, string operationId)
    {
        try
        {
            using (tcpClient)
            await using (var stream = tcpClient.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            await using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                stream.ReadTimeout = 300000; // 5 minutes
                stream.WriteTimeout = 300000;

                // Lire la requête (format: "operation|filePath|key")
                var request = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(request))
                {
                    await writer.WriteLineAsync("ERROR|2|Requête vide");
                    return;
                }

                var parts = request.Split('|');
                if (parts.Length < 3)
                {
                    await writer.WriteLineAsync("ERROR|2|Format invalide. Attendu: operation|filePath|key");
                    return;
                }

                var operation = parts[0].ToLowerInvariant();
                var filePath = parts[1];
                var key = parts[2];

                Console.WriteLine($"📥 Requête reçue: {operation} {Path.GetFileName(filePath)}");

                // Attendre le sémaphore pour garantir mono-instance des encryptions
                Console.WriteLine("   ⏳ En attente du verrou mono-instance...");
                await _encryptionSemaphore.WaitAsync(_cts.Token);

                try
                {
                    Console.WriteLine("   🔒 Verrou acquis, traitement en cours...");
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    int exitCode;

                    if (operation == "encrypt")
                    {
                        exitCode = AesGcmEncryptor.EncryptFile(filePath, key);
                    }
                    else if (operation == "decrypt")
                    {
                        exitCode = AesGcmEncryptor.DecryptFile(filePath, key);
                    }
                    else
                    {
                        await writer.WriteLineAsync($"ERROR|2|Opération inconnue: {operation}");
                        return;
                    }

                    stopwatch.Stop();

                    if (exitCode == 0)
                    {
                        await writer.WriteLineAsync($"OK|0|{stopwatch.ElapsedMilliseconds}");
                        Console.WriteLine($"   ✓ {operation} terminé en {stopwatch.ElapsedMilliseconds}ms");
                    }
                    else
                    {
                        await writer.WriteLineAsync($"ERROR|{exitCode}|{GetErrorMessage(exitCode)}");
                        Console.WriteLine($"   ✗ {operation} échoué (code {exitCode})");
                    }
                }
                finally
                {
                    _encryptionSemaphore.Release();
                    Console.WriteLine("   🔓 Verrou libéré");
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("   ⚠ Opération annulée (shutdown)");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"   ✗ Erreur: {ex.Message}");
        }
        finally
        {
            _activeOperations.TryRemove(operationId, out _);
        }
    }

    private static string GetErrorMessage(int code)
    {
        return code switch
        {
            1 => "Fichier introuvable",
            2 => "Arguments invalides",
            3 => "Erreur I/O",
            4 => "Échec authentification GCM",
            5 => "Clé invalide",
            _ => $"Erreur inconnue ({code})"
        };
    }
}

