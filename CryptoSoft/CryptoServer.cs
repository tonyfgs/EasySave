using System.IO.Pipes;
using System.Text;
using System.Collections.Concurrent;

namespace CryptoSoft;

public class CryptoServer
{
    private const string PipeName = "CryptoSoftPipe";
    private const string MutexName = "Global\\CryptoSoftServerMutex";
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, Task> _activeOperations = new();

    public static bool IsServerRunning()
    {
        try
        {
            using var mutex = Mutex.OpenExisting(MutexName);
            return true;
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            return false;
        }
    }

    public int Run()
    {
        // Vérifier qu'on est la seule instance serveur
        using var mutex = new Mutex(true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            Console.Error.WriteLine("CryptoSoft Server est déjà en cours d'exécution.");
            return 6;
        }

        Console.WriteLine("🔐 CryptoSoft Server démarré");
        Console.WriteLine($"   Pipe: {PipeName}");
        Console.WriteLine("   En attente de connexions...");
        Console.WriteLine("   Appuyez sur Ctrl+C pour arrêter.");
        Console.WriteLine();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            _cts.Cancel();
            Console.WriteLine("\n⏹ Arrêt demandé...");
        };

        try
        {
            // Accepter plusieurs connexions en parallèle
            while (!_cts.Token.IsCancellationRequested)
            {
                var serverPipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous);

                try
                {
                    serverPipe.WaitForConnection();

                    // Traiter chaque connexion dans un thread séparé
                    Task.Run(() => HandleClient(serverPipe), _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    serverPipe.Dispose();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erreur serveur: {ex.Message}");
            return 3;
        }

        // Attendre que toutes les opérations en cours se terminent
        Task.WaitAll(_activeOperations.Values.ToArray(), TimeSpan.FromSeconds(30));

        Console.WriteLine("✓ CryptoSoft Server arrêté proprement.");
        return 0;
    }

    private void HandleClient(NamedPipeServerStream pipe)
    {
        var operationId = Guid.NewGuid().ToString();
        _activeOperations[operationId] = Task.CompletedTask;

        try
        {
            using (pipe)
            {
                using var reader = new StreamReader(pipe, Encoding.UTF8);
                using var writer = new StreamWriter(pipe, Encoding.UTF8) { AutoFlush = true };

                // Lire la requête (format: "operation|filePath|key")
                var request = reader.ReadLine();
                if (string.IsNullOrEmpty(request))
                {
                    writer.WriteLine("ERROR|2|Requête vide");
                    return;
                }

                var parts = request.Split('|');
                if (parts.Length < 3)
                {
                    writer.WriteLine("ERROR|2|Format invalide. Attendu: operation|filePath|key");
                    return;
                }

                var operation = parts[0].ToLowerInvariant();
                var filePath = parts[1];
                var key = parts[2];

                Console.WriteLine($"📥 Requête reçue: {operation} {Path.GetFileName(filePath)}");

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
                    writer.WriteLine($"ERROR|2|Opération inconnue: {operation}");
                    return;
                }

                stopwatch.Stop();

                if (exitCode == 0)
                {
                    writer.WriteLine($"OK|0|{stopwatch.ElapsedMilliseconds}");
                    Console.WriteLine($"   ✓ {operation} terminé en {stopwatch.ElapsedMilliseconds}ms");
                }
                else
                {
                    writer.WriteLine($"ERROR|{exitCode}|{GetErrorMessage(exitCode)}");
                    Console.WriteLine($"   ✗ {operation} échoué (code {exitCode})");
                }
            }
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

