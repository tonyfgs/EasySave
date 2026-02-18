namespace CryptoSoft;

class Program
{
    private const string MutexName = "Global\\CryptoSoftMutex";

    static int Main(string[] args)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("  CryptoSoft v1.0 - AES-256-GCM Encryption Tool");
        Console.WriteLine("  Développé pour EasySave");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine();

        if (args.Length == 0)
        {
            ShowUsage();
            return 2;
        }

        string operation = args[0].ToLowerInvariant();

        // Génération de clé et help n'ont pas besoin du mutex
        if (operation == "genkey")
        {
            return HandleGenerateKey();
        }

        if (operation == "help")
        {
            ShowUsage();
            return 0;
        }

        // Pour encrypt/decrypt, utiliser le mutex mono-instance
        return RunWithMutex(args, operation);
    }

    private static int RunWithMutex(string[] args, string operation)
    {
        using var mutex = new Mutex(false, MutexName, out bool createdNew);
        
        // Si le mutex existe déjà, essayer de l'acquérir immédiatement
        if (!createdNew)
        {
            try
            {
                if (!mutex.WaitOne(0))
                {
                    Console.Error.WriteLine("CryptoSoft is already running");
                    return 6;
                }
            }
            catch (AbandonedMutexException)
            {
                // Le mutex a été abandonné par un autre processus, on peut continuer
            }
        }

        try
        {
            return ExecuteOperation(args, operation);
        }
        finally
        {
            try
            {
                mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Le mutex n'était pas possédé, ignorer
            }
        }
    }

    private static int ExecuteOperation(string[] args, string operation)
    {
        // Chiffrement/déchiffrement nécessitent 3 arguments
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Erreur : Nombre d'arguments insuffisant.");
            Console.Error.WriteLine();
            ShowUsage();
            return 2;
        }

        string filePath = args[1];
        string keyBase64 = args[2];

        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            Console.Error.WriteLine("Erreur : La clé de chiffrement ne peut pas être vide.");
            return 2;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Console.Error.WriteLine("Erreur : Le chemin du fichier ne peut pas être vide.");
            return 2;
        }

        return operation switch
        {
            "encrypt" => HandleEncrypt(filePath, keyBase64),
            "decrypt" => HandleDecrypt(filePath, keyBase64),
            _ => HandleUnknownOperation(operation)
        };
    }

    private static int HandleGenerateKey()
    {
        string newKey = AesGcmEncryptor.GenerateKey();
        Console.WriteLine("✓ Nouvelle clé AES-256 générée :");
        Console.WriteLine();
        Console.WriteLine($"  {newKey}");
        Console.WriteLine();
        Console.WriteLine("⚠ IMPORTANT : Sauvegardez cette clé dans un endroit sûr !");
        Console.WriteLine("  Cette clé sera nécessaire pour déchiffrer vos fichiers.");
        Console.WriteLine("  CryptoSoft ne stocke aucune clé - c'est votre responsabilité.");
        return 0;
    }

    private static int HandleEncrypt(string filePath, string keyBase64)
    {
        Console.WriteLine($"🔒 Chiffrement de : {filePath}");
        Console.WriteLine();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        int exitCode = AesGcmEncryptor.EncryptFile(filePath, keyBase64);
        stopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine($"⏱ Temps de chiffrement : {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"Code de retour : {exitCode} ({GetExitCodeDescription(exitCode)})");

        return exitCode;
    }

    private static int HandleDecrypt(string filePath, string keyBase64)
    {
        Console.WriteLine($"🔓 Déchiffrement de : {filePath}");
        Console.WriteLine();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        int exitCode = AesGcmEncryptor.DecryptFile(filePath, keyBase64);
        stopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine($"⏱ Temps de déchiffrement : {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"Code de retour : {exitCode} ({GetExitCodeDescription(exitCode)})");

        return exitCode;
    }

    private static int HandleUnknownOperation(string operation)
    {
        Console.Error.WriteLine($"Erreur : Opération inconnue '{operation}'");
        Console.Error.WriteLine();
        ShowUsage();
        return 2;
    }

    private static void ShowUsage()
    {
        Console.WriteLine("UTILISATION :");
        Console.WriteLine();
        Console.WriteLine("  Génération de clé :");
        Console.WriteLine("    CryptoSoft.exe genkey");
        Console.WriteLine("    → Génère une nouvelle clé AES-256. Sauvegardez-la vous-même !");
        Console.WriteLine();
        Console.WriteLine("  Chiffrement :");
        Console.WriteLine("    CryptoSoft.exe encrypt \"C:\\dossier\\fichier.pdf\" \"cléBase64_32octets==\"");
        Console.WriteLine("    → Crée fichier.pdf.crypt");
        Console.WriteLine();
        Console.WriteLine("  Déchiffrement :");
        Console.WriteLine("    CryptoSoft.exe decrypt \"C:\\dossier\\fichier.pdf.crypt\" \"cléBase64_32octets==\"");
        Console.WriteLine("    → Recrée fichier.pdf");
        Console.WriteLine();
        Console.WriteLine("  Aide :");
        Console.WriteLine("    CryptoSoft.exe help");
        Console.WriteLine();
        Console.WriteLine("CODES DE RETOUR :");
        Console.WriteLine("  0 - Succès");
        Console.WriteLine("  1 - Fichier source introuvable / non lisible");
        Console.WriteLine("  2 - Arguments invalides (mauvais nombre, clé vide)");
        Console.WriteLine("  3 - Erreur entrée/sortie (disque plein, permissions...)");
        Console.WriteLine("  4 - Échec d'authentification GCM (mauvaise clé ou fichier altéré)");
        Console.WriteLine("  5 - Clé invalide (pas exactement 32 octets après base64)");
        Console.WriteLine("  6 - Instance déjà en cours d'exécution");
        Console.WriteLine();
        Console.WriteLine("SÉCURITÉ :");
        Console.WriteLine("  • Algorithme : AES-256-GCM (mode AEAD)");
        Console.WriteLine("  • Clé : 256 bits (32 octets) en Base64");
        Console.WriteLine("  • Nonce : 12 octets aléatoires par fichier");
        Console.WriteLine("  • Tag : 16 octets d'authentification");
        Console.WriteLine("  • Standard : NIST/ANSSI 2026");
        Console.WriteLine("  • Support : Fichiers de toute taille");
        Console.WriteLine("  • Nettoyage automatique : Suppression artefacts en cas d'échec");
        Console.WriteLine("  • Mono-instance : Une seule exécution simultanée autorisée");
        Console.WriteLine();
        Console.WriteLine("GESTION DES CLÉS :");
        Console.WriteLine("  • CryptoSoft ne stocke AUCUNE clé");
        Console.WriteLine("  • Vous devez sauvegarder vos clés vous-même");
        Console.WriteLine("  • Perdre une clé = perdre l'accès aux fichiers chiffrés");
    }

    private static string GetExitCodeDescription(int code)
    {
        return code switch
        {
            0 => "Succès",
            1 => "Fichier introuvable",
            2 => "Arguments invalides",
            3 => "Erreur I/O",
            4 => "Échec authentification GCM",
            5 => "Clé invalide",
            6 => "Instance déjà en cours",
            _ => "Code inconnu"
        };
    }
}
